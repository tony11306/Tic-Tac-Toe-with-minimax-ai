using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace _004 {
    // 寫一寫發現耦合度超高...
    public partial class Form1 : Form {

        private Game game = null;
        private HumanPlayer humanPlayer = null;
        private AI ai = null;
        private List<List<Button>> buttons;
        private Timer timer = new Timer();
        public Form1() {
            InitializeComponent();
            buttons = new List<List<Button>> { 
                new List<Button> { button1, button2, button3 },
                new List<Button> { button4, button5, button6 },
                new List<Button> { button7, button8, button9 },
            };
            humanPlayer = new HumanPlayer(Symbol.CIRCLE);
            ai = new AI(Symbol.CROSS, buttons);
            game = new Game(ai, humanPlayer); // 前 O player, 後 X player, 若要對調記得修改
            for (int i = 0; i < Game.BOARD_SIZE; ++i) {
                for (int j = 0; j < Game.BOARD_SIZE; ++j) {
                    buttons[i][j].Text = "";
                    // can't directly use i j for some reasons, I think it's probably related to reference stuff >:(
                    int row = i; 
                    int col = j;
                    buttons[i][j].Click += (sender, e) => { 
                        game.buttonEvent(sender, e, row, col);
                        if (game.checkIsGameOver()) {
                            return;
                        }
                        currentPlayerLabel.Text = "Current Player: " + (game.getCurrentPlayer() == humanPlayer ? "You" : "AI");
                    };
                }
            }

            timer.Interval = 100;
            timer.Tick += timerTick;
            currentPlayerLabel.Text = "Current Player: " + (game.getCurrentPlayer() == humanPlayer ? "You" : "AI");
            timer.Start();

        }



        private void timerTick(object sender, EventArgs e) {

            if (game.checkIsGameOver()) {
                timer.Stop();
                if (game.getWinner() == null) {
                    currentPlayerLabel.Text = "Tie game";
                } else {
                    currentPlayerLabel.Text = "Winner: " + (game.getWinner() == humanPlayer ? "You" : "AI");
                }
                return;
            }
            
        }

        private void Form1_Load(object sender, EventArgs e) {
            if (game.getCurrentPlayer().isAI()) {
                ai.play(game);
            }
        }
    }

    public enum Symbol {
        EMPTY = 0,
        CROSS = 'O',
        CIRCLE = 'X'
    }

    public interface IPlayer {
        void play(Game game);
        bool isAI();

        Symbol getSymbol();

    }

    public partial class HumanPlayer : IPlayer {

        private Symbol symbolSide;
        public HumanPlayer(Symbol symbolSide) {
            this.symbolSide = symbolSide;
        }
        public void play(Game game) {

        }

        public bool isAI() {
            return false;
        }

        public Symbol getSymbol() {
            return symbolSide;
        }


    }

    public partial class AI : IPlayer {
        private Symbol symbolSide;
        private int DEPTH = 9;
        private List<List<Button>> buttons;
        public AI(Symbol symbolSide, List<List<Button>> buttons) {
            this.symbolSide = symbolSide;
            this.buttons = buttons;
        }
        public int evaluate(Game game, bool isMaxing=false, int currentDepth = 0) { // minimax algorithm

            Symbol[][] board = game.getBoard();
            if (currentDepth >= DEPTH) {
                return 0;
            }

            if (isLose(game)) {
                return -100;
            } else if (isWin(game)) {
                return 50;
            } else if (isTie(board)) {
                return 1;
            }

            List<int> scores = new List<int>();
            for (int i = 0; i < Game.BOARD_SIZE; ++i) {
                for (int j = 0; j < Game.BOARD_SIZE; ++j) {
                    if (board[i][j] == Symbol.EMPTY) {
                        if (getSymbol() == Symbol.CROSS) {
                            board[i][j] = isMaxing ? Symbol.CROSS : Symbol.CIRCLE;
                        } else {
                            board[i][j] = isMaxing ? Symbol.CIRCLE : Symbol.CROSS;
                        }
                        scores.Add(evaluate(game, !isMaxing, currentDepth+1));
                        board[i][j] = Symbol.EMPTY;
                    }
                }
            }
            if (isMaxing) {
                return scores.Max();
            }
            return scores.Min();
        }

        public bool isWin(Game game) {
            return game.getWinner() == this;
        }

        public bool isLose(Game game) {
            IPlayer winner = game.getWinner();
            return winner != null && winner != this;
        }

        public bool isTie(Symbol[][] board) {
            for (int i = 0; i < Game.BOARD_SIZE; ++i) {
                for (int j = 0; j < Game.BOARD_SIZE; ++j) {
                    if (board[i][j] == Symbol.EMPTY) {
                        return false;
                    }
                }
            }
            return true;
        }
        public void play(Game game) {
            // score row col
            List<Tuple<int, int, int>> possibleMoves = new List<Tuple<int, int, int>>();
            Symbol[][] board = game.getBoard();
            for (int i = 0; i < Game.BOARD_SIZE; ++i) {
                for (int j = 0; j < Game.BOARD_SIZE; ++j) {
                    if (board[i][j] == Symbol.EMPTY) {
                        board[i][j] = getSymbol();
                        Tuple<int, int, int> possibleMove = new Tuple<int, int, int>(evaluate(game), i, j);
                        board[i][j] = Symbol.EMPTY;
                        possibleMoves.Add(possibleMove);
                    }
                }
            }
            Console.WriteLine("----- AI Decision -----");
            foreach (Tuple<int, int, int> m in possibleMoves) {
                Console.WriteLine("move " + m.Item2 + "/" + m.Item3 + ", score: " + m.Item1);
            }

            Tuple<int, int, int> bestMove = possibleMoves.Max();
            buttons[bestMove.Item2][bestMove.Item3].PerformClick();
            // Console.WriteLine("AI Clicked" + bestMove.Item2 + " " + bestMove.Item3);

        }
        public bool isAI() {
            return true;
        }
        public Symbol getSymbol() {
            return symbolSide;
        }
    }

    public partial class Game {
        public const int BOARD_SIZE = 3;
        private Symbol[][] board = new Symbol[BOARD_SIZE][];
        private IPlayer crossPlayer = null; // cross side
        private IPlayer circlePlayer = null; // circle side
        private IPlayer currentPlayer = null;
        private bool isGameOver = false;

        /*
         (0,0), (0,1), (0,2)
         (1,0), (1,1), (1,2)
         (2,0), (2,1), (2,2)
         */

        public Game(IPlayer crossPlayer, IPlayer circlePlayer) { // 前 O 後 X 重要!!
            this.crossPlayer = crossPlayer;
            this.circlePlayer = circlePlayer;
            this.currentPlayer = this.circlePlayer;
            for (int i = 0; i < BOARD_SIZE; ++i) {
                board[i] = new Symbol[BOARD_SIZE];
            }
        }

        public void buttonEvent(object sender, EventArgs e, int row, int col) {

            if (isGameOver) {
                return;
            }

            if (board[row][col] != Symbol.EMPTY) {
                return;
            }
            board[row][col] = currentPlayer.getSymbol();
            ((Button)sender).Text = board[row][col] == Symbol.CIRCLE ? "O" : "X";

            if (getWinner() != null || isTie()) {
                isGameOver = true;
                return;
            }

            switchCurrentPlayer();

            if (currentPlayer.isAI()) {
                ((AI)currentPlayer).play(this);
            }

            
            
            

        }

        private void switchCurrentPlayer() {
            currentPlayer = currentPlayer == crossPlayer ? circlePlayer : crossPlayer;
        }
        private bool isCrossWin() { // O(n^2)
            // check all rows
            for (int row = 0; row < BOARD_SIZE; ++row) {
                bool isWin = true;
                for (int col = 0; col < BOARD_SIZE; ++col) {
                    if (board[row][col] != Symbol.CROSS) {
                        isWin = false;
                        break;
                    }
                }
                if (isWin) {
                    return true;
                }
            }

            // check all cols
            for (int col = 0; col < BOARD_SIZE; ++col) {
                bool isWin = true;
                for (int row = 0; row < BOARD_SIZE; ++row) {
                    if (board[row][col] != Symbol.CROSS) {
                        isWin = false;
                        break;
                    }
                }
                if (isWin) {
                    return true;
                }
            }

            // check diagonals
            bool isDiagonal = true;
            for (int i = 0; i < BOARD_SIZE; ++i) {
                if (board[i][i] != Symbol.CROSS) {
                    isDiagonal = false;
                    break;
                }
            }

            if (isDiagonal) {
                return true;
            }
            isDiagonal = true;
            for (int i = 0; i < BOARD_SIZE; ++i) {
                if (board[BOARD_SIZE - i - 1][i] != Symbol.CROSS) {
                    isDiagonal = false;
                    break;
                }
            }

            if (isDiagonal) {
                return true;
            }

            return false;
        }
        private bool isCircleWin() { // O(n^2)
            // check all rows
            for (int row = 0; row < BOARD_SIZE; ++row) {
                bool isWin = true;
                for (int col = 0; col < BOARD_SIZE; ++col) {
                    if (board[row][col] != Symbol.CIRCLE) {
                        isWin = false;
                        break;
                    }
                }
                if (isWin) {
                    return true;
                }
            }

            // check all cols
            for (int col = 0; col < BOARD_SIZE; ++col) {
                bool isWin = true;
                for (int row = 0; row < BOARD_SIZE; ++row) {
                    if (board[row][col] != Symbol.CIRCLE) {
                        isWin = false;
                        break;
                    }
                }
                if (isWin) {
                    return true;
                }
            }

            // check diagonals
            bool isDiagonal = true;
            for (int i = 0; i < BOARD_SIZE; ++i) {
                if (board[i][i] != Symbol.CIRCLE) {
                    isDiagonal = false;
                    break;
                }
            }

            if (isDiagonal) {
                return true;
            }

            isDiagonal = true;

            for (int i = 0; i < BOARD_SIZE; ++i) {
                if (board[BOARD_SIZE - i - 1][i] != Symbol.CIRCLE) {
                    isDiagonal = false;
                    break;
                }
            }

            if (isDiagonal) {
                return true;
            }

            return false;
        }

        private bool isTie() {
            for (int i = 0; i < Game.BOARD_SIZE; ++i) {
                for (int j = 0; j < Game.BOARD_SIZE; ++j) {
                    if (board[i][j] == Symbol.EMPTY) {
                        return false;
                    }
                }
            }
            return true;
        }
        public IPlayer getWinner() {
            if (isCrossWin()) {
                return crossPlayer;
            } else if (isCircleWin()) {
                return circlePlayer;
            }

            return null;
        }

        public Symbol[][] getBoard() {
            return board;
        }
        public IPlayer getCurrentPlayer() {
            return currentPlayer;
        }

        public bool checkIsGameOver() {
            return isGameOver;
        }



    }
    
}
