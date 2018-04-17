using System;
using System.Collections.Generic;

namespace OthelloIA_G3
{
    class TreeNode
    {
        public enum EType { MIN = -1, MAX = 1 };

        // Attributs

        public EType Type { get; set; }

        private Board board;
        private bool isWhite;

        /// <summary>
        /// Constructeur de TreeNode
        /// </summary>
        /// <param name="board">Board.</param>
        /// <param name="type">Type.</param>
        /// <param name="isWhite">If set to <c>true</c>, current player is white.</param>
        public TreeNode(Board board, EType type, bool isWhite)
        {
            this.board = new Board(board);
            Type = type;
            this.isWhite = isWhite;
        }

        /// <summary>
        /// Evaluation du board avec plusieurs coefficients heuristiques.
        /// Ces coefficients sont déterminés selon des techniques permettant
        /// d'indiquer quels facteurs de jeu garanti un meilleur 
        /// pourcentage de réussite.
        /// </summary>
        public double Eval()
        {
            //Déclaration des variables
            int maxPlayerCoin, minPlayerCoin, maxPlayerMove, minPlayerMove, coinParity,
            mobility, whiteCorners, blackCorners, whiteStability, blackStability,
            maxCorners, minCorners, corners, maxStability, minStability, stability;

            //Initialisation des variables
            maxPlayerCoin = minPlayerCoin = maxPlayerMove = minPlayerMove = coinParity =
            mobility = whiteCorners = blackCorners = whiteStability = blackStability =
            maxCorners = minCorners = corners = maxStability = minStability = stability = 0;

            //Initialisation de la matrice de valeurs pour valoriser les coins. 
            int[,] mat_corner_weight = new int[,] {{20, -10, 1, 1, 1, 1, -10, 20},
                                                 {-10, -7, 1, 1, 1, 1, -7, -10},
                                                 {1, 1, 1, 1, 1, 1, 1, 1},
                                                 {1, 1, 1, 1, 1, 1, 1, 1},
                                                 {1, 1, 1, 1, 1, 1, 1, 1},
                                                 {1, 1, 1, 1, 1, 1, 1, 1},
                                                 {-10, -7, 1, 1, 1, 1, -7, -10},
                                                 {20, -10, 1, 1, 1, 1, -10, 20}};

            //Initialiation de la matrice de stabilité
            //La stabilité est définie en fonction du fait qu'un pion pourra être
            //retourné dans le tour même, dans un des tours à venir ou qu'il soit
            //impossible à être retourné.
            int[,] mat_stability_weight = new int[,] { {4, -3, 2, 2, 2, 2, -3, 4},
                                                     {-3, -4, -1, -1, -1, -1, -4, -3},
                                                     {2, -1, 1, 0, 0, 1, -1, 2},
                                                     {2,  -1,  0,  1,  1,  0, -1,  2},
                                                     {2,  -1,  0,  1,  1,  0, -1,  2 },
                                                     {2,  -1,  1,  0,  0,  1, -1,  2},
                                                     {-3, -4, -1, -1, -1, -1, -4, -3},
                                                     {4,  -3,  2,  2,  2,  2, -3,  4}};


            //Récupère le nombre de pions des joueurs à maximiser et à minimiser
            maxPlayerCoin = isWhite ? board.GetWhiteScore() : board.GetBlackScore();
            minPlayerCoin = isWhite ? board.GetBlackScore() : board.GetWhiteScore();

            //Retourne une valeur entre -100 et 100 du rapport du nombre de pion à maximiser sur le total des deux joueurs
            if (maxPlayerCoin > minPlayerCoin && (maxPlayerCoin + minPlayerCoin) != 0)
            {
                coinParity = (100 * maxPlayerCoin) / (maxPlayerCoin + minPlayerCoin);
            }
            else if (maxPlayerCoin < minPlayerCoin && (maxPlayerCoin + minPlayerCoin) != 0)
            {
                coinParity = -(100 * minPlayerCoin) / (maxPlayerCoin + minPlayerCoin);
            }

            //Nombre de coups possibles des blancs et des noirs
            int nbrOfPlayableMoveWhite = Ops(true).Count;
            int nbrOfPlayableMoveBlack = Ops(false).Count;

            //Assigne le nombre de coups en fonction de qui maximiser/minimiser
            maxPlayerMove = isWhite ? nbrOfPlayableMoveWhite : nbrOfPlayableMoveBlack;
            minPlayerMove = isWhite ? nbrOfPlayableMoveBlack : nbrOfPlayableMoveWhite;

            //Retourne une valeur entre -100 et 100 du rapport du nombre de coups à maximiser sur le total des deux joueurs
            if (maxPlayerMove > minPlayerMove && (maxPlayerMove + minPlayerMove) != 0)
            {
                mobility = (100 * maxPlayerMove) / (maxPlayerMove + minPlayerMove);
            }
            else if (maxPlayerMove < minPlayerMove && (maxPlayerMove + minPlayerMove) != 0)
            {
                mobility = -(100 * minPlayerMove) / (maxPlayerMove + minPlayerMove);
            }

            //Retourne une valeur en fonction des pions posés sur le damier
            whiteCorners = EvaluateMatriceValue(mat_corner_weight, true);
            blackCorners = EvaluateMatriceValue(mat_corner_weight, false);

            //Détermine quel poids de case est adressé au joueur à maximiser
            maxCorners = isWhite ? whiteCorners : blackCorners;
            minCorners = isWhite ? blackCorners : whiteCorners;

            //Retourne une valeur entre -100 et 100 du rapport du poids des cases à maximiser sur le total des deux joueurs

            if (maxCorners > minCorners && (maxCorners + minCorners) != 0)
            {
                corners = (100 * maxCorners) / (maxCorners + minCorners);
            }
            else if (maxCorners < minCorners && (maxCorners + minCorners) != 0)
            {
                corners = -(100 * minCorners) / (maxCorners + minCorners);
            }

            //Retourne une valeur en fonction de la stabilité des pions placés sur le damier
            whiteStability = EvaluateMatriceValue(mat_stability_weight, true);
            blackStability = EvaluateMatriceValue(mat_stability_weight, false);

            //Détermine quelle stabilité est adressé au joueur à maximiser
            maxStability = isWhite ? whiteStability : blackStability;
            minStability = isWhite ? blackStability : whiteStability;

            //Retourne une valeur entre -100 et 100 du rapport de stabilité à maximiser sur le total des deux joueurs
            if (maxStability > minStability && (maxStability + minStability) != 0)
            {
                stability = (100 * maxStability) / (maxStability + minStability);
            }
            else if (maxStability < minStability && (maxStability + minStability) != 0)
            {
                stability = -(100 * minStability) / (maxStability + minStability);
            }

            //Retourne ces dernières valeurs avec des coéfficients selon leur importance
            return 2 * coinParity + 5 * mobility + 10 * corners + 15 * stability;
        }

        /// <summary>
        /// Calcul le poids des cases de la matrice passée en argument
        /// en fonction de la présence d'un pion noir ou blanc. 
        /// Le paramètre white permet de définir ce paramètre
        /// </summary>
        /// <returns>The matrice value.</returns>
        /// <param name="mat_eval">Mat eval.</param>
        /// <param name="white">If set to <c>true</c> white.</param>
        private int EvaluateMatriceValue(int[,] mat_eval, bool white)
        {
            int typeCoin = white ? 0 : 1;
            int value = 0;

            for (int c = 0; c < 8; c++)
            {
                for (int l = 0; l < 8; l++)
                {
                    if (board.GetBoard()[c, l] == typeCoin)
                    {
                        value += mat_eval[c, l];
                    }
                }
            }
            return value;
        }

        /// <summary>
        /// Check si la partie est terminée en fonction du plateau de jeu passé en paramètres
        /// </summary>
        /// <param name="board">Etat du plateau de jeu</param>
        /// <returns>True si la partie est terminée</returns>
        public static bool Final(Board board)
        {
            for (int c = 0; c < 8; c++)
            {
                for (int l = 0; l < 8; l++)
                {
                    if (board.GetBoard()[c, l] > -1)
                        // Cellule prise
                        continue;
                    else
                    {
                        if (board.IsPlayable(c, l, true) || board.IsPlayable(c, l, false))
                            // Un des deux joueurs peut encore jouer
                            return false;
                    }
                }
            }
            // Aucune possibilité
            return true;
        }

        /// <summary>
        /// Fait appel à Final static avec le board de l'instance.
        /// </summary>
        /// <returns></returns>
        public bool Final()
        {
            return Final(board);
        }

        /// <summary>
        /// Liste les coups jouables par un des deux joueurs d'après le plateau de jeu
        /// </summary>
        /// <param name="isWhite">Couleur du joueur. True = joueur blanc</param>
        /// <returns>Liste des coups possibles de ce joueur</returns>
        public List<Cell> Ops(bool isWhite)
        {
            var ops = new List<Cell>();
            for (int c = 0; c < 8; c++)
            {
                for (int l = 0; l < 8; l++)
                {
                    if (board.IsPlayable(c, l, isWhite))
                    {
                        ops.Add(new Cell(c, l));
                    }
                }
            }
            return ops;
        }

        /// <summary>
        /// Joue un coup
        /// </summary>
        /// <returns>Un nouvel arbre de coup possible d'après l'état du plateau après le coup joué</returns>
        public TreeNode Apply(int col, int line, bool isWhite)
        {
            Board newBoard = new Board(board);
            newBoard.PlayMove(col, line, isWhite);

            EType newType;
            if (Type == EType.MAX)
                newType = EType.MIN;
            else
                newType = EType.MAX;

            return new TreeNode(newBoard, newType, this.isWhite);
        }

        /// <summary>
        /// Représentation du plateau de jeu
        /// </summary>
        public override string ToString()
        {
            string output = "";
            for (int l = 0; l < 8; l++)
            {
                for (int c = 0; c < 8; c++)
                {
                    output += board.GetBoard()[c, l] + " ";
                }
                output += "\n";
            }
            return output;
        }
    }
}
