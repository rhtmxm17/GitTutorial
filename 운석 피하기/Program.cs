namespace ConsoleProject2
{
    internal class Program
    {
        /* 피하기 게임
         
        상하로 캐릭터(@)를 이동하며 오른쪽에서 왼쪽으로 날아오는 물체(<>)를 피한다
        반각 크기로 이동시키기 위해 날아오는 물체는 가로로

        소코반에서 시험해본 일정 주기마다 입력을 확인하는 코드를 게임루프에 활용

        아이템($)과 충돌시 추가 점수 획득 및 일정 확률로 생명 추가 
         
         */

        const int INTERVAL = 2;
        const int LINES = 7;
        const int STONS = 15;

        public struct GameData
        {
            public bool running;
            public ConsoleKey inputKey;

            public int playerPos;
            public int life;
            public int score;

            public Point[] stons;
            public Point[] items;
        }

        // 점수와 생명을 표시할 콘솔 좌표
        public struct CursorData
        {
            public Point life;
            public Point score;
        }

        public struct Point
        {
            public int x;
            public int y;
        }

        static GameData data;
        static CursorData cursor;
        static Random random;

        static void Main(string[] args)
        {
            Start();
            while (data.running)
            {
                Thread.Sleep(100);
                Input();
                Update();
                Render();
            }
            End();
        }

        static void Start()
        {
            Console.CursorVisible = false;
            random = new Random();

            data = new GameData
            {
                running = true,
                inputKey = ConsoleKey.None,
                playerPos = 2,
                life = 2,
                score = 0,
                stons = new Point[STONS],
                items = new Point[1],
            };


            for (int i = 0; i < data.stons.Length; i++)
            {
                data.stons[i].x = 20 + INTERVAL * i;
                data.stons[i].y = random.Next(LINES);
            }

            for (int i = 0; i < data.items.Length; i++)
            {
                data.items[i].x = 20 + i;
                data.items[i].y = random.Next(LINES);
            }

            PrintMap();
        }

        static void End()
        {
            Console.SetCursorPosition(0, LINES + 4);
            Console.WriteLine($"게임 종료!");
        }

        /// <summary>
        /// 키 입력이 있을 경우 inputKey 그 정보를에 담는다
        /// 없다면 None을 담는다
        /// 주의: 꾹 누르는 입력을 할 경우 입력이 쌓인 만큼 다 처리해야 다음 입력이 가능한 상태
        /// </summary>
        static void Input()
        {
            if (Console.KeyAvailable)
            {
                data.inputKey = Console.ReadKey(true).Key;
            }
            else
            {
                // 입력중이 아니라면 None
                data.inputKey = ConsoleKey.None;
            }
        }

        static void Update()
        {
            KeyCheck();
            MoveAndCheckDrops();
            StageCheck();
        }

        static void Render()
        {
            ClearMap();
            PrintDrops();
            PrintPlayer();
            PrintScore();
        }

        static void PrintMap()
        {
            Console.Clear();
            Console.WriteLine("=#==================================");
            for (int i = 0; i < LINES; i++)
            {
                Console.WriteLine(" :");
            }
            Console.WriteLine("=#==================================");

            // 점수 안내 텍스트와 그 값을 출력할 커서 위치를 저장해둔다
            Console.Write($"Life: ");
            (cursor.life.x, cursor.life.y) = Console.GetCursorPosition();
            Console.WriteLine();
            Console.Write($"Score: ");
            (cursor.score.x, cursor.score.y) = Console.GetCursorPosition();
        }

        static void PrintScore()
        {
            Console.SetCursorPosition(cursor.life.x, cursor.life.y);
            Console.Write($"{data.life,-2}");
            Console.WriteLine();
            Console.SetCursorPosition(cursor.score.x, cursor.score.y);
            Console.Write($"{data.score,+4}점");
        }

        // 운석이 날아오는 공간에 공백을 출력하는 함수
        // 맵을 통째로 재 출력하는 대신에 공백 출력을 사용해서 깜빡임을 우회함
        static void ClearMap()
        {
            Console.SetCursorPosition(0, 1);
            for (int i = 0; i < LINES; i++)
            {
                Console.WriteLine(" :                                                                            ");
            }
        }

        static void PrintDrops()
        {
            for (int i = 0; i < data.items.Length; i++)
            {
                Console.SetCursorPosition(data.items[i].x + 1, data.items[i].y + 1);
                Console.Write("$");
            }

            for (int i = 0; i < data.stons.Length; i++)
            {
                Console.SetCursorPosition(data.stons[i].x + 1, data.stons[i].y + 1);
                Console.Write("<>");
            }
        }

        static void PrintPlayer()
        {
            Console.SetCursorPosition(1, data.playerPos + 1);
            Console.Write("@");
        }



        static void KeyCheck()
        {
            switch (data.inputKey)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    data.playerPos--;
                    if (data.playerPos < 0)
                        data.playerPos = 0;
                    break;
                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    data.playerPos++;
                    if (data.playerPos > LINES - 1)
                        data.playerPos = LINES - 1;
                    break;
            }
        }

        static void MoveAndCheckDrops()
        {
            for (int i = 0; i < data.stons.Length; i++)
            {
                data.stons[i].x--;
                if (data.stons[i].x < 0)
                {
                    data.stons[i].x = INTERVAL * data.stons.Length;
                    data.stons[i].y = random.Next(LINES);

                    // 돌 하나 재배치 할 때 마다 점수 증가
                    data.score++;
                }
                else if (data.stons[i].x == 0)
                {
                    if (data.stons[i].y == data.playerPos)
                        data.life--;
                }
            }

            for (int i = 0; i < data.items.Length; i++)
            {
                data.items[i].x--;
                if (data.items[i].x < 0)
                {
                    data.items[i].x = 30;
                    data.items[i].y = random.Next(LINES);
                }
                else if (data.items[i].x == 0)
                {
                    // 아이템 습득시 10점 추가 및 일정 확률로 라이프 증가
                    if (data.items[i].y == data.playerPos)
                    {
                        data.score += 10;
                        if(random.Next() % 5 == 0)
                            data.life++;
                    }
                }
            }
        }

        static void StageCheck()
        {
            if (data.life <= 0)
                data.running = false;
        }
    }
}
