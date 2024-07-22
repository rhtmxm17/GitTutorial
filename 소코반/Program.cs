using System.Diagnostics;

namespace ConsoleProject1
{
    internal class Program
    {
        /* Push Push 게임
        Maze 샘플 기반

        타일에 통과 가능/불가능 외에 추가 정보가 필요하므로 map은 enum으로 변경

        맵 요소: 벽, 이동 가능 공간, 골 지점
        물체: 플레이어, 공

        이동
            1. 목표 지점이 벽이면 이동 불가
            2. 목표 지점이 비었으면 이동 가능
            3. 목표 지점이 공이면
                a. 공의 목표 지점이 벽이면 이동 불가
                b. 공의 목표 지점이 공이면 이동 불가
                c. 둘 다 아니면 공과 플레이어 모두 이동
                    (공의 이동 지점이 골이면 상태 변경)
        
        출력
            맵 출력 후 플레이어 정보 덮어쓰기
            공, 골 정보는 맵 정보에 포함시켰다

        스테이지 선택
        스테이지 게임루프 바깥에 스테이지 선택 루프 구축

         */

        public enum Tile
        {
            /// <summary>이동 가능한 빈 타일</summary>
            Road,
            Goal,
            Ball,
            /// <summary>골에 공이 들어가있는 경우</summary>
            Full,
            Wall,
        }

        public enum Direction
        {
            Up,
            Down,
            Left,
            Right,
            /// <summary>방향 없음</summary>
            NONE,
        }

        public struct GameData
        {
            public bool gameRunning;
            public bool stageRunning;
            public int stage;
            public ConsoleKey inputKey;

            public MapData mapData;
        }

        public struct MapData
        {
            public Tile[,] map;
            /// <summary>남은 공 개수</summary>
            public int balls;
            public Point playerPos;
        }

        public struct Point
        {
            public int x;
            public int y;
        }

        static GameData data;
        static Stopwatch stopwatch;

        static void Main(string[] args)
        {
            Start();
            while (data.gameRunning)
            {
                StageSelect();

                while (data.stageRunning)
                {
                    Render();
                    Input();
                    Update();
                }
            }
        }

        static void Start()
        {
            Console.CursorVisible = false;

            data = new GameData
            {
                gameRunning = true,
                stageRunning = false
            };
            stopwatch = new Stopwatch();
        }

        static void StageSelect()
        {
            Console.Clear();
            Console.WriteLine("스테이지 번호(1 ~ 3)를 선택하세요. 게임종료: 0");
            bool isCorrectInput = false;

            while (isCorrectInput == false)
            {
                isCorrectInput = true;
                switch (Console.ReadKey(false).Key)
                {
                    case ConsoleKey.D1:
                        data.stage = 1;
                        break;
                    case ConsoleKey.D2:
                        data.stage = 2;
                        break;
                    case ConsoleKey.D3:
                        data.stage = 3;
                        break;
                    case ConsoleKey.D0:
                        data.stage = 0;
                        data.gameRunning = false;
                        break;
                    default:
                        Console.SetCursorPosition(0, 1);
                        isCorrectInput = false;
                        break;
                }
            }

            data.stageRunning = data.stage > 0;

            // 스테이지가 선택되었을 경우 선택된 스테이지 정보 불러오기
            if (data.stageRunning)
            {
                SetupMapData(out data.mapData, data.stage);
            }
        }

        /// <summary>
        /// 스테이지 정보에 기반해 콘솔 출력
        /// </summary>
        static void Render()
        {
            Console.Clear();

            PrintMap();
            PrintPlayer();
            PrintInformation();
        }

        static void Input()
        {
            bool blinkToggle = false;

            while (true)
            {
                // 타이머 (재)시작
                stopwatch.Restart();

                // 타이머 대기
                while (stopwatch.ElapsedMilliseconds < 500)
                {
                    // 키 입력시 대기 중단
                    if (Console.KeyAvailable != false)
                        break;
                }
                if (Console.KeyAvailable != false)
                    break;

                blinkToggle = !blinkToggle;
                if(blinkToggle)
                {
                    PrintPlayer(ConsoleColor.Cyan);
                }
                else
                {
                    PrintPlayer(ConsoleColor.DarkCyan);
                }
            }
            data.inputKey = Console.ReadKey(true).Key;
        }

        /// <summary>
        /// 키 입력에 기반해 스테이지 정보 갱신
        /// </summary>
        static void Update()
        {
            KeyCheck();
            CheckStageClear();
        }

        static void KeyCheck()
        {
            // 키 입력이 방향을 의미할 경우 해당 방향을 넣어준다
            // 그 외의 입력일 경우 NONE을 유지한다
            Direction inputDirection = Direction.NONE;
            switch (data.inputKey)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    inputDirection = Direction.Up;
                    break;
                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    inputDirection = Direction.Down;
                    break;
                case ConsoleKey.A:
                case ConsoleKey.LeftArrow:
                    inputDirection = Direction.Left;
                    break;
                case ConsoleKey.D:
                case ConsoleKey.RightArrow:
                    inputDirection = Direction.Right;
                    break;
                // 초기화: R
                case ConsoleKey.R:
                    // 맵 정보를 다시 불러와서 스테이지 초기화
                    SetupMapData(out data.mapData, data.stage);
                    break;
                // 스테이지 선택: Esc
                case ConsoleKey.Escape:
                    data.stageRunning = false;
                    break;
                default:
                    return;
            }

            // 방향 입력이 있었을 경우 이동 함수 수행
            if (inputDirection != Direction.NONE)
            {
                PlayerMove(inputDirection);
            }
        }

        /// <summary>
        /// 방향을 입력받아 플레이어 이동을 시도한다.
        /// 해당 방향에 공이 있을 경우 공 또한 이동을 시도한다.
        /// </summary>
        /// <param name="direction">이동할 방향</param>
        static void PlayerMove(Direction direction)
        {
            Point next = NextPoint(data.mapData.playerPos, direction);

            switch (data.mapData.map[next.y, next.x])
            {
                case Tile.Road:
                case Tile.Goal:
                    data.mapData.playerPos = next;
                    break;
                // 공을 미는 경우
                case Tile.Ball:
                    {
                        // 공이 밀릴 경우의 위치
                        Point ballNext = NextPoint(next, direction);
                        switch (data.mapData.map[ballNext.y, ballNext.x])
                        {
                            case Tile.Road:
                                data.mapData.playerPos = next;
                                data.mapData.map[next.y, next.x] = Tile.Road;
                                data.mapData.map[ballNext.y, ballNext.x] = Tile.Ball;
                                break;
                            case Tile.Goal:
                                data.mapData.playerPos = next;
                                data.mapData.map[next.y, next.x] = Tile.Road;
                                data.mapData.map[ballNext.y, ballNext.x] = Tile.Full;
                                data.mapData.balls--;
                                break;
                            case Tile.Ball:
                            case Tile.Full:
                            case Tile.Wall:
                                break;
                        }
                    }
                    break;
                // 골에 들어있는 공울 미는 경우
                case Tile.Full:
                    {
                        // 공이 밀릴 경우의 위치
                        Point ballNext = NextPoint(next, direction);
                        switch (data.mapData.map[ballNext.y, ballNext.x])
                        {
                            case Tile.Road:
                                data.mapData.playerPos = next;
                                data.mapData.map[next.y, next.x] = Tile.Goal;
                                data.mapData.map[ballNext.y, ballNext.x] = Tile.Ball;
                                // 골에서 공이 빠져나왔으므로 남은 공 증가
                                data.mapData.balls++;
                                break;
                            case Tile.Goal:
                                data.mapData.playerPos = next;
                                data.mapData.map[next.y, next.x] = Tile.Goal;
                                data.mapData.map[ballNext.y, ballNext.x] = Tile.Full;
                                break;
                            case Tile.Ball:
                            case Tile.Full:
                            case Tile.Wall:
                                break;
                        }
                    }
                    break;
                // 목적지가 벽일 경우 이동하지 않는다
                case Tile.Wall:
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 스테이지 클리어 여부 확인
        /// </summary>
        static void CheckStageClear()
        {
            if (data.mapData.balls <= 0)
            {
                data.stageRunning = false;

                // 클리어 메세지 출력 전, 마지막 이동을 반영하기 위해 한번 더 Render()을 호출함
                Render();

                // 클리어 메세지를 스테이지 중앙에, 출력 시작 위치를 짝수 칸으로 맞추기 위함
                if (data.mapData.map.GetLength(1) % 2 == 1)
                {
                    Console.SetCursorPosition(data.mapData.map.GetLength(1) - 5, data.mapData.map.GetLength(0) / 2);
                    Console.Write("[클리어!!]");
                }
                else
                {
                    Console.SetCursorPosition(data.mapData.map.GetLength(1) - 6, data.mapData.map.GetLength(0) / 2);
                    Console.Write(" [클리어!!] ");
                }
                Console.ReadKey(true);

            }
        }

        static void PrintMap()
        {
            for (int y = 0; y < data.mapData.map.GetLength(0); y++)
            {
                for (int x = 0; x < data.mapData.map.GetLength(1); x++)
                {
                    // 해당 타일의 속성에 따라 출력
                    switch (data.mapData.map[y, x])
                    {
                        case Tile.Road:
                            Console.Write("  ");
                            break;
                        case Tile.Goal:
                            Console.Write("○");
                            break;
                        case Tile.Ball:
                            Console.Write("●");
                            break;
                        case Tile.Full:
                            Console.Write("◎");
                            break;
                        case Tile.Wall:
                            Console.Write("▦");
                            break;
                    }
                }
                Console.WriteLine();
            }
        }

        static void PrintPlayer(ConsoleColor color = ConsoleColor.DarkCyan)
        {
            int oldTop = Console.GetCursorPosition().Top;
            int oldLeft = Console.GetCursorPosition().Left;

            // 플레이어 위치로 커서 이동
            // 특수문자가 콘솔 커서 2칸을 사용해서 x * 2
            Console.SetCursorPosition(data.mapData.playerPos.x * 2, data.mapData.playerPos.y);
            Console.ForegroundColor = color;
            Console.Write("ⓟ");
            Console.ResetColor();

            Console.SetCursorPosition(oldLeft, oldTop);
        }

        static void PrintInformation()
        {
            // 스테이지에서 한줄 띄운 위치로 커서 이동
            Console.SetCursorPosition(0, data.mapData.map.GetLength(0) + 1);

            Console.WriteLine($"남은공: {data.mapData.balls}개");
            Console.WriteLine($"초기화: R");
            Console.WriteLine($"스테이지 선택: Esc");
        }

        /// <summary>
        /// 입력된 좌표에서 입력된 방향으로 한칸 이동한 좌표 반환
        /// </summary>
        /// <param name="point">기준 좌표</param>
        /// <param name="direction">대상 방향</param>
        /// <returns>이동한 좌표</returns>
        static Point NextPoint(Point point, Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    point.y--;
                    break;
                case Direction.Down:
                    point.y++;
                    break;
                case Direction.Left:
                    point.x--;
                    break;
                case Direction.Right:
                    point.x++;
                    break;
            }
            return point;
        }

        /// <summary>
        /// 스테이지 번호를 받아 맵 데이터를 출력
        /// </summary>
        /// <param name="data">출력할 맵 데이터</param>
        /// <param name="stage">스테이지 번호</param>
        static void SetupMapData(out MapData data, int stage)
        {
            data = new MapData();
            switch (stage)
            {
                case 1:
                    data.map = new Tile[,]
                    {
                        {Tile.Road, Tile.Road, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Road, Tile.Road, Tile.Road, },
                        {Tile.Road, Tile.Road, Tile.Wall, Tile.Goal, Tile.Wall, Tile.Road, Tile.Road, Tile.Road, },
                        {Tile.Road, Tile.Road, Tile.Wall, Tile.Road, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Wall, },
                        {Tile.Wall, Tile.Wall, Tile.Wall, Tile.Ball, Tile.Road, Tile.Ball, Tile.Goal, Tile.Wall, },
                        {Tile.Wall, Tile.Goal, Tile.Road, Tile.Ball, Tile.Road, Tile.Wall, Tile.Wall, Tile.Wall, },
                        {Tile.Wall, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Ball, Tile.Wall, Tile.Road, Tile.Road, },
                        {Tile.Road, Tile.Road, Tile.Road, Tile.Wall, Tile.Goal, Tile.Wall, Tile.Road, Tile.Road, },
                        {Tile.Road, Tile.Road, Tile.Road, Tile.Wall, Tile.Road, Tile.Wall, Tile.Road, Tile.Road, },
                        {Tile.Road, Tile.Road, Tile.Road, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Road, Tile.Road, },
                    };
                    data.balls = 4;
                    data.playerPos = new Point { x = 4, y = 4 };
                    break;

                case 2:
                    data.map = new Tile[,]
                    {
                        {Tile.Wall, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Road, Tile.Road, Tile.Road, Tile.Road, },
                        {Tile.Wall, Tile.Road, Tile.Road, Tile.Road, Tile.Wall, Tile.Road, Tile.Road, Tile.Road, Tile.Road, },
                        {Tile.Wall, Tile.Road, Tile.Ball, Tile.Ball, Tile.Wall, Tile.Road, Tile.Wall, Tile.Wall, Tile.Wall, },
                        {Tile.Wall, Tile.Road, Tile.Ball, Tile.Road, Tile.Wall, Tile.Road, Tile.Wall, Tile.Goal, Tile.Wall, },
                        {Tile.Wall, Tile.Wall, Tile.Wall, Tile.Road, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Goal, Tile.Wall, },
                        {Tile.Road, Tile.Wall, Tile.Wall, Tile.Road, Tile.Road, Tile.Road, Tile.Road, Tile.Goal, Tile.Wall, },
                        {Tile.Road, Tile.Wall, Tile.Road, Tile.Road, Tile.Road, Tile.Wall, Tile.Road, Tile.Road, Tile.Wall, },
                        {Tile.Road, Tile.Wall, Tile.Road, Tile.Road, Tile.Road, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Wall, },
                        {Tile.Road, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Road, Tile.Road, Tile.Road, },
                        {Tile.Road, Tile.Road, Tile.Road, Tile.Road, Tile.Road, Tile.Road, Tile.Road, Tile.Road, Tile.Road, },
                    };
                    data.balls = 3;
                    data.playerPos = new Point { x = 1, y = 1 };
                    break;

                case 3:
                    data.map = new Tile[,]
                    {
                        {Tile.Road, Tile.Road, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Road, },
                        {Tile.Wall, Tile.Wall, Tile.Wall, Tile.Road, Tile.Road, Tile.Road, Tile.Wall, Tile.Road, },
                        {Tile.Wall, Tile.Goal, Tile.Road, Tile.Ball, Tile.Road, Tile.Road, Tile.Wall, Tile.Road, },
                        {Tile.Wall, Tile.Wall, Tile.Wall, Tile.Road, Tile.Ball, Tile.Goal, Tile.Wall, Tile.Road, },
                        {Tile.Wall, Tile.Goal, Tile.Wall, Tile.Wall, Tile.Ball, Tile.Road, Tile.Wall, Tile.Road, },
                        {Tile.Wall, Tile.Road, Tile.Wall, Tile.Road, Tile.Goal, Tile.Road, Tile.Wall, Tile.Wall, },
                        {Tile.Wall, Tile.Ball, Tile.Road, Tile.Full, Tile.Ball, Tile.Ball, Tile.Goal, Tile.Wall, },
                        {Tile.Wall, Tile.Road, Tile.Road, Tile.Road, Tile.Goal, Tile.Road, Tile.Road, Tile.Wall, },
                        {Tile.Wall, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Wall, Tile.Wall, },
                    };
                    data.balls = 6;
                    data.playerPos = new Point { x = 2, y = 2 };
                    break;
            }

        }
    }
}
