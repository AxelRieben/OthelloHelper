using System;
using System.Collections.Generic;

namespace OthelloIA_G3
{
    public class Board : IPlayable.IPlayable
    {
        // ATTRIBUTS
        private int[,] board;
        private static string NAME = "Costa_Renaud";

        // CONSTRUCTEURS

        /// <summary>
        /// Construcuteur sans paramètres.
        /// </summary>
        public Board()
        {
            // Init board
            board = new int[8, 8];
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    board[i, j] = -1;
                }
            }

            // Placement des premiers pions
            board[3, 3] = board[4, 4] = 0; // Blanc
            board[4, 3] = board[3, 4] = 1; // Noir
        }

        public bool IsFinished()
        {
            return TreeNode.Final(this);
        }

        public Board(int[,] game)
        {
            // Init board
            board = new int[8, 8];

            // Copy source values            
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    board[i, j] = game[i, j];
                }
            }
        }

        /// <summary>
        /// Constructeur de copie.
        /// </summary>
        /// <param name="source"></param>
        public Board(Board source) : this(source.GetBoard()) { }

        // IMPLEMENTS

        /// <summary>
        /// Retourne le nom de l'IA.
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return NAME;
        }

        /// <summary>
        /// Retourne le tableau de int représentant le board.
        /// </summary>
        /// <returns></returns>
        public int[,] GetBoard()
        {
            return board;
        }

        /// <summary>
        /// Calcul le score des pions noir.
        /// </summary>
        /// <returns></returns>
        public int GetBlackScore()
        {
            int score = 0;
            foreach (int i in board)
            {
                if (i == 1)
                    score++;
            }
            return score;
        }

        /// <summary>
        /// Calcul le score des pions blancs.
        /// </summary>
        /// <returns></returns>
        public int GetWhiteScore()
        {
            int score = 0;
            foreach (int i in board)
            {
                if (i == 0)
                    score++;
            }
            return score;
        }

        /// <summary>
        /// Retourne le prochain coup que l'IA va jouer.
        /// </summary>
        /// <param name="game">game board</param>
        /// <param name="level">niveau de profondeur</param>
        /// <param name="whiteTurn">qui joue ?</param>
        /// <returns></returns>
        public Tuple<int, int> GetNextMove(int[,] game, int level, bool whiteTurn)
        {
            int myScore = whiteTurn ? GetWhiteScore() : GetBlackScore();

            TreeNode root = new TreeNode(this, TreeNode.EType.MAX, whiteTurn);

            var bestOp = AlphaBeta(root, level, whiteTurn, (int)TreeNode.EType.MAX, -int.MaxValue).Item2;
            //Console.WriteLine($"{bestOp.C}, {bestOp.L}");

            if (bestOp == null)
            {
                bestOp = new Cell(-1, -1);
            }
            return bestOp.ToTuple();
        }

        /// <summary>
        /// Retourne si oui ou non un coup est jouable par un joueur d'une certaine couleur.
        /// </summary>
        /// <returns>True si le coup est jouable</returns>
        public bool IsPlayable(int column, int line, bool isWhite)
        {
            return CheckOrPlay(column, line, isWhite, true);
        }

        /// <summary>
        /// Joue un coup et met à jour le plateau de jeu.
        /// </summary>
        /// <returns>True si le coup a pu être joué</returns>
        public bool PlayMove(int column, int line, bool isWhite)
        {
            return CheckOrPlay(column, line, isWhite, false);
        }

        // PRIVATE

        /// <summary>
        /// Méthode générique appelée par IsPlayable et PlayMove.
        /// </summary>
        /// <param name="checkOnly">
        /// True si l'on veut juste vérifier qu'un coup est jouable. 
        /// False si on veut jouer le coup et mettre à jour le plateau.
        /// </param>
        /// <returns>True si le coup est jouable / a été joué</returns>
        private bool CheckOrPlay(int column, int line, bool isWhite, bool checkOnly)
        {
            // List de tous les pions à retourner
            List<Pawn> toFlip = new List<Pawn>();

            // Check case is empty
            if (board[column, line] > -1)
            {
                return false;
            }

            bool playable = false;

            // Check voisin d'autre couleurs
            int colorOpponent = isWhite ? 1 : 0;
            int colorPlayer = isWhite ? 0 : 1;

            //  (c ; l) 
            //  -----------------------
            //  (-1;-1)  (0;-1)  (1;-1)
            //  (-1; 0)  (0; 0)  (1; 0)
            //  (-1; 1)  (0; 1)  (1; 1)

            // Init start/end for line and column
            int cstart = column <= 0 ? 0 : -1;
            int cend = column >= 7 ? 0 : 1;
            int lstart = line <= 0 ? 0 : -1;
            int lend = line >= 7 ? 0 : 1;

            // Parcours des voisins
            for (int c = cstart; c <= cend; c++)
            {
                for (int l = lstart; l <= lend; l++)
                {
                    // Check seulement si la case n'est pas nous-même
                    if (!(c == 0 && l == 0))
                    {
                        // Check si le voisin est un pion adverse
                        if (board[column + c, line + l] == colorOpponent)
                        {
                            // List des pions adverses sur cette ligne/colonne/diagonale
                            List<Pawn> opponentPion = new List<Pawn>
                            {
                                new Pawn(column + c, line + l, colorOpponent)
                            };

                            // Check si il y a un pion de notre couleur dans cette ligne/colonne/diagonale
                            int copyC = c + c; // c dans [-1;1]
                            int copyL = l + l; // l dans [-1;1]

                            // Tant que les index des lignes et colonnes ne sont pas en dehors du plateau
                            while (column + copyC < 8 && line + copyL < 8 && column + copyC >= 0 && line + copyL >= 0)
                            {
                                int currentCell = board[column + copyC, line + copyL];
                                if (currentCell == colorPlayer)
                                {
                                    // Un pion au joueur actuel. Le coup est jouable. On passe à la suite.
                                    if (checkOnly)
                                    {
                                        // Au moins un coup est jouable
                                        return true;
                                    }
                                    else
                                    {
                                        // On ajoute tous les pions adverses de cette ligne/colonne/diagonale dans la liste des pions à retourner.
                                        playable = true;
                                        foreach (var tuple in opponentPion)
                                        {
                                            toFlip.Add(tuple);
                                        }
                                        break;
                                    }
                                }
                                else if (currentCell < 0)
                                {
                                    // Case vide. Rien de concluant dans cette ligne/colonne/diagonale. On passe à la suite.
                                    break;
                                }
                                else if (currentCell == colorOpponent)
                                {
                                    // Pion adversaire. On l'ajoute à la liste de pions adverses de cette ligne/colonne/diagonale.
                                    opponentPion.Add(new Pawn(column + copyC, line + copyL, colorOpponent));
                                }

                                // Déplacement dans la continuité (ligne ou colonne ou diagonale)
                                copyC += c;
                                copyL += l;
                            }
                        }
                    }
                }
            }
            if (!checkOnly && playable)
            {
                // Si le coup est jouable et que l'on souhaite jouer le coup, on retourne tous les pions adverses
                // et on ajoute un pion.
                toFlip.ForEach(pawn => board[pawn.C, pawn.L] = colorPlayer);
                board[column, line] = colorPlayer;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Algorithme AlphaBeta pour choisir le meilleur coup à jouer
        /// </summary>
        /// <param name="root">Noeud de l'arbre des coups jouables</param>
        /// <param name="level">Nombre de coup à jouer en avance</param>
        /// <param name="isWhite">Couleur du joueur</param>
        /// <param name="minOrMax">Type du noeud</param>
        /// <param name="parentValue">Valeur du noeau parent</param>
        /// <returns></returns>
        private Tuple<double, Cell> AlphaBeta(TreeNode root, int level, bool isWhite, int minOrMax, double parentValue)
        {
            // Si dernier niveau ou que la partie est terminée, on fait appel à la fonction d'évaluation du plateau
            if (level == 0 || root.Final())
            {
                return Tuple.Create<double, Cell>(root.Eval(), null);
            }

            // Valeur optimale (initialisation à la pire valeur possible)
            double optVal = minOrMax * -int.MaxValue;

            // Coups jouables
            var ops = root.Ops(isWhite);

            // Coup optimal (initialisation à null -> passe son tour)
            Cell optOp = null;

            if (ops.Count > 0)
            {
                // Si au moins un coup est jouable, initialisation avec le premier coup trouvé
                optOp = ops[0];
            }

            // Pas besoin de chercher le meilleur coup si un seul est possible
            if (ops.Count > 1)
            {
                // Parcours de tout les coup afin de trouver les meilleur
                foreach (var op in ops)
                {
                    // Nouvel arbre obtenu après avoir jouer le coup courant
                    TreeNode newTree = root.Apply(op.C, op.L, isWhite);

                    // Alpha beta récursif
                    double val = AlphaBeta(newTree, level - 1, !isWhite, -minOrMax, optVal).Item1;

                    // Meilleur résultat trouvé, affectation comme coup et val optimales.
                    if (val * minOrMax > optVal * minOrMax)
                    {
                        optVal = val;
                        optOp = op;

                        if (optVal * minOrMax > parentValue * minOrMax)
                            break;
                    }
                }
            }
            return Tuple.Create(optVal, optOp);
        }
    }
}


