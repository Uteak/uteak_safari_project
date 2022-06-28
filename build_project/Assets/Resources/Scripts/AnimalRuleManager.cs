using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Resources.Scripts;
using System.Diagnostics;
using System;
using System.Text;
//��ü���� ���� �����ϴ� Ŭ����


//mcts --> �� �Ŵ��� �����Ͽ��� ������ �׼� �˷���


//      x:0,    x:1 ,    x2:
//y:0  [0,0]   [0,1]   [0,2]
//y:1  [1,0]   [1,1]   [1,2]
//y:2  [2,0]   [2,1]   [2,2]
//y:3  [3,0]   [3,1]   [3,2]


//  1 ,2 ,3
//a 1a 2a 3a
//b 1b 2b 3b
//c 1c 2c 3c
//d 1d 2d 3d
//[ y , x ]



//      x:0,    x:1 ,    x2:
//y:0  [0,0]   [0,1]   [0,2] 
//y:1  [1,0]   [1,1]   [1,2]
//y:2  [2,0]   [2,1]   [2,2]
//y:3  [3,0]   [3,1]   [3,2]

//  1 ,2 ,3
//a 1a 2a 3a
//b 1b 2b 3b
//c 1c 2c 3c
//d 1d 2d 3d 

//[ y , x ] 

namespace Assets.Resources.Scripts
{


    public class AnimalRuleManager : MonoBehaviour
    {

        public Environment env = null;


        //# move direction
        static (int dirY, int dirX) UL = (-1, -1);
        static (int dirY, int dirX) UU = (-1, 0);
        static (int dirY, int dirX) UR = (-1, 1);
        static (int dirY, int dirX) ML = (0, -1);
        static (int dirY, int dirX) MR = (0, 1);
        static (int dirY, int dirX) DL = (1, -1);
        static (int dirY, int dirX) DD = (1, 0);
        static (int dirY, int dirX) DR = (1, 1);


        Dictionary<int, (int dirY, int dirX)[]> ALLOWED_MOVES = new Dictionary<int, (int dirY, int dirX)[]>()
        {
            [Environment.L1] = new (int dirY, int dirX)[] { UL, UU, UR, ML, MR, DL, DD, DR},
            [Environment.L2] = new (int dirY, int dirX)[] { UL, UU, UR, ML, MR, DL, DD, DR},
            [Environment.E1] = new (int dirY, int dirX)[] { UL, UR, DL, DR },
            [Environment.E2] = new (int dirY, int dirX)[] { UL, UR, DL, DR },
            [Environment.G1] = new (int dirY, int dirX)[] { UU, ML, MR, DD },
            [Environment.G2] = new (int dirY, int dirX)[] { UU, ML, MR, DD },
            [Environment.P1] = new (int dirY, int dirX)[] { UU },
            [Environment.P2] = new (int dirY, int dirX)[] { DD },
            [Environment.C1] = new (int dirY, int dirX)[] { UL, UU, UR, ML, MR, DD },
            [Environment.C2] = new (int dirY, int dirX)[] { DL, DD, DR, ML, MR, UU },

        };



        //public bool isEnvironmentInit = true;

        public int gameID = 0;

        private Piece SelectedPiece = null;

        private string start_pos_str = "";

        public Dictionary<SharedDataType.EColor, SafariAgent> Agent = new Dictionary<SharedDataType.EColor, SafariAgent>();

        public bool EndepiosodeState = false;


        public SharedDataType.EColor beforeColor = SharedDataType.EColor.Count;

        public SharedDataType.EColor eCurrentTurn = SharedDataType.EColor.White;


        public void InitializeAgent()
        {
            Agent[SharedDataType.EColor.Black] = CreateAgent("BlackAgent", SharedDataType.EColor.Black);
            Agent[SharedDataType.EColor.White] = CreateAgent("WhiteAgent", SharedDataType.EColor.White);

        }

        public SafariAgent CreateAgent(string objectName, SharedDataType.EColor colorType)
        {
            GameObject agentObj = new GameObject(objectName);
            agentObj.transform.parent = transform;
            agentObj.transform.localPosition = Vector3.zero;
            agentObj.SetActive(false);

            SafariAgent agent = agentObj.AddComponent<SafariAgent>();
            agent.InitializeAgent(this, objectName, colorType);

            agentObj.SetActive(true);
            return agent;
        }

        public void Initialize(Environment animalGame)
        {
            env = animalGame;
        }

        public void SetPlayer()
        {
            if (Agent.Count == 0)
                return;

            if (GameManager.instance.HumanColor == SharedDataType.EColor.White)
            {
                Agent[SharedDataType.EColor.Black].enabled = true;
            }
            else if (GameManager.instance.HumanColor == SharedDataType.EColor.Black)
            {
                Agent[SharedDataType.EColor.White].enabled = true;

            }
            else if (GameManager.instance.HumanColor == SharedDataType.EColor.Count)
            {
                if (Unity.MLAgents.Academy.Instance.IsCommunicatorOn == false)
                {
                    return;
                }
                Agent[SharedDataType.EColor.White].enabled = true;
                Agent[SharedDataType.EColor.Black].enabled = true;
            }
        }

        private void OnEnable()
        {

            SetPlayer();
            EndepiosodeState = false;

        }

        private void OnDisable()
        {
            EndepiosodeState = true;
            
            Agent[SharedDataType.EColor.White].enabled = false;
            Agent[SharedDataType.EColor.Black].enabled = false;
        }

        private void Awake()
        {
            GameObject environment = new GameObject("Environment");
            environment.transform.parent = transform;
            environment.transform.localPosition = new Vector3(0, 0, 0);
            Environment env = new Environment();
            Initialize(env);
            env.Initialize(this, environment.transform);

            

        }

        // Start is called before the first frame update
        void Start()
        {

            //Agent ����
            InitializeAgent();
            ResetGame();
            
        }





        // Update is called once per frame
        void Update()
        {
            //�н���
            //if(Agent[eCurrentTurn].behaviorParameters.BehaviorType == Unity.MLAgents.Policies.BehaviorType.Default)
            //{
            //UnityEngine.Debug.Log(Agent[eCurrentTurn].GetCumulativeReward());

            if (EndepiosodeState == true)
            {
                if( Agent[SharedDataType.EColor.Black].enabled )
                {
                    Agent[SharedDataType.EColor.Black].EndEpisode();
                }

                if( Agent[SharedDataType.EColor.White].enabled )
                {
                    Agent[SharedDataType.EColor.White].EndEpisode();
                }
                
                ResetGame();
                EndepiosodeState = false;
            }

            if (Agent[eCurrentTurn].enabled)
            {

                if(eCurrentTurn == beforeColor)
                {
                    return;
                }

                Agent[eCurrentTurn].RequestDecision();
                beforeColor = eCurrentTurn;

            }
            
            //ChangeTurn();
            //}




            //isEnvironmentInit = false;

            //�ش� �ǽ��� ���� ������ְ� ũ��� ������� 
            //���콺 �����ǿ� �ٰ���
            //�н��� �ּ�
            if (SelectedPiece != null)
            {
                if (CheckTurn(SelectedPiece.eColor) == false)
                {
                    //non select 
                    return;
                }

                SelectedPiece.transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }







        }

        public void ResetGame()
        {
            //isEnvironmentInit = true;

            Agent[SharedDataType.EColor.Black].enabled = false;
            Agent[SharedDataType.EColor.White].enabled = false;

            eCurrentTurn = SharedDataType.EColor.White;
            beforeColor = SharedDataType.EColor.Count;
            //GameObject envObj = env.GetEnvironmentObj();
            //Destroy(envObj);

            //GameObject environment = new GameObject("Environment");
            //environment.transform.parent = transform;
            //environment.transform.localPosition = new Vector3(0, 0, 0);
            env.ResetEnvironment();
            //Environment newEnv = new Environment();
            //Initialize(newEnv);
            
            //env.Initialize(this, env.GetEnvironmentObj(). transform);

            SetPlayer();


        }



        //�ش� �׼��� �������� Ȯ���ϴ� �޼ҵ�
        public bool CheckAction(double action)
        {
            Dictionary<double, double> allAction = GetAvailableAllActions();

            if (allAction.ContainsKey(action) == false)
            {
                return false;
            }

            return true;
        }

        public enum EActionType
        {
            Stock,
            Piece,
            None,
        }

        public enum EGameState
        {
            Continue,
            CaptureChick,
            CaptureChicken,
            CaptureGiraph,
            CaptureElephant,
            OnBoardChick,
            OnBoardGiraph,
            OnBoardElephant,
            Promotion,
            ThreatLion,
            StupidAction,
            Win,
            ERROR_ACTION,
        }

        public EGameState SetActionMove(string start, string end)
        {
            EActionType actionType = EActionType.Piece;
            EGameState result = EGameState.Continue;

            //���ο� �⹰ ������ check
            if (start.Length < 2)
            {
                actionType = EActionType.Stock;
            }

            //(int X, int Y) target_pos = GetPositionFromString(end);
            (int Y, int X) target_pos = GameManager.StringPosToIntPos[end];


            BoardSlot targetSlotScript = env.BoardSlots[target_pos.Y, target_pos.X];



            switch (actionType)
            {
                case EActionType.Piece:
                    {
                        // target�ǽ��� ���� �𸣴µ� �ϴ� ������
                        Piece targetPiece = targetSlotScript.GetPiece();
                        //���� ��ġ�� �ִ� �⹰�� piece�� �θ� ���� 
                        //(int X, int Y) start_pos = GetPositionFromString(start);
                        (int Y, int X) start_pos = GameManager.StringPosToIntPos[start];
                        BoardSlot startSlotScript = env.BoardSlots[start_pos.Y, start_pos.X];

                        if (startSlotScript.GetPiece() == null )
                        {
                            return EGameState.ERROR_ACTION;
                        }
                        if (startSlotScript.GetPiece().eColor != eCurrentTurn)
                        {
                            return EGameState.ERROR_ACTION;
                        }


                        

                        if (targetPiece != null) //������ Ÿ���ǽ��� �⹰�� �ִ� ���
                        {
                            //��� �⹰�� �ִ� ���
                            (int color, int pieceID) stockID = BoardStock.GetStockID(targetPiece.pieceID);

                            int currentTurn = (int)eCurrentTurn;
                            //stock �ϳ� �ø���
                            if (env.BoardStocks[currentTurn, stockID.pieceID].Count < 9)
                            {
                                env.BoardStocks[currentTurn, stockID.pieceID].Count += 1;
                                env.BoardStocks[currentTurn, stockID.pieceID].SetCount(env.BoardStocks[currentTurn, stockID.pieceID].Count);
                            }

                            //���� ���� 
                            if (targetPiece.pieceID == Environment.L1 || targetPiece.pieceID == Environment.L2)
                            {
                                result = EGameState.Win;
                                targetSlotScript.DestoryHasPiece();
                                return result;
                            }

                            if (targetPiece.pieceID == Environment.P1 || targetPiece.pieceID == Environment.P2)
                            {
                                result = EGameState.CaptureChick;
                            }
                            else if (targetPiece.pieceID == Environment.C1 || targetPiece.pieceID == Environment.C2)
                            {
                                result = EGameState.CaptureChicken;
                            }
                            else if (targetPiece.pieceID == Environment.G1 || targetPiece.pieceID == Environment.G2)
                            {
                                result = EGameState.CaptureGiraph;
                            }
                            else if (targetPiece.pieceID == Environment.E1 || targetPiece.pieceID == Environment.E2)
                            {
                                result = EGameState.CaptureElephant;
                            }

                            //���� �ǽ� ����
                            targetSlotScript.DestoryHasPiece();

                        }


                        Piece startPiece = startSlotScript.DetachPiece();

                        if(startPiece == null)
                        {
                            return EGameState.ERROR_ACTION;
                        }

                        //���Ƹ��� ������ ������ ���
                        //���   ���Ƹ��� y == 0�� �� 
                        //������  ���Ƹ��� y== 3�϶� 
                        if (target_pos.Y == 0 && startPiece.pieceID == Environment.P1)
                        {
                            startPiece.Promotion();
                            result = EGameState.Promotion;
                        }
                        if (target_pos.Y == 3 && startPiece.pieceID == Environment.P2)
                        {
                            startPiece.Promotion();
                            result = EGameState.Promotion;
                        }

                        if (target_pos.Y == 3 && startPiece.pieceID == Environment.L2)
                        {
                            //������ ����
                            result = EGameState.Win;
                            Destroy(startPiece.gameObject);
                            return result;
                        }

                        if (target_pos.Y == 0 && startPiece.pieceID == Environment.L1)
                        {
                            //������ ����
                            result = EGameState.Win;
                            Destroy(startPiece.gameObject);
                            return result;
                        }


                        //���� 
                        targetSlotScript.SetPiece(startPiece);
                        startPiece.transform.parent = targetSlotScript.transform;
                        startPiece.transform.localPosition = Vector3.zero;

                        break;
                    }


                case EActionType.Stock:
                    {
                        //SourcePiece
                        (int color, int pieceID) stockID = GetStockFromString(start);
                        Piece sourcePiece = env.BoardStocks[stockID.color, stockID.pieceID].GetPiece();

                        if (sourcePiece == null)
                        {
                            return EGameState.ERROR_ACTION;
                        }

                        if (sourcePiece.eColor != eCurrentTurn)
                        {
                            return EGameState.ERROR_ACTION;
                        }


                        GameObject newPiece = PieceManager.instance.CreatePiece(sourcePiece.pieceID);
                        Piece newPieceScript = newPiece.GetComponent<Piece>();


                        targetSlotScript.SetPiece(newPieceScript);
                        newPiece.transform.localScale = new Vector3(1, 1, 0);
                        newPiece.transform.parent = targetSlotScript.transform;
                        newPiece.transform.localPosition = Vector3.zero;


                        //���� ī���� ���� 
                        env.BoardStocks[stockID.color, stockID.pieceID].Count -= 1;
                        env.BoardStocks[stockID.color, stockID.pieceID].SetImage(env.BoardStocks[stockID.color, stockID.pieceID].Count);

                        if (newPieceScript.pieceID == Environment.P1 || newPieceScript.pieceID == Environment.P2)
                        {
                            result = EGameState.OnBoardChick;
                        }
                        else if (newPieceScript.pieceID == Environment.G1 || newPieceScript.pieceID == Environment.G2)
                        {
                            result = EGameState.OnBoardGiraph;
                        }
                        else if (newPieceScript.pieceID == Environment.E1 || newPieceScript.pieceID == Environment.E2)
                        {
                            result = EGameState.OnBoardElephant;
                        }


                        break;
                    }
            }

            //// ���⹰�� �����ΰ�? 
            //if (targetSlotScript.GetPiece().pieceID == Environment.L1 || targetSlotScript.GetPiece().pieceID == Environment.L2)
            //{
            //    //�����ε�, ���� �⹰�� �����Ÿ��� �� ���� �ִ� ��� 
            //    foreach (BoardSlot slot in env.BoardSlots)
            //    {
            //        if (slot.GetPiece() == null)
            //        {
            //            continue;
            //        }

            //        if (slot.GetPiece().eColor != eCurrentTurn)
            //        {
            //            //�ش� ������ �⹰�� �����̴�.
            //            //������ �����Ÿ��� ���� ���� �������� �ִ°�? 
            //            //�ǽ��� ��� ��θ� �����ϴ� �Լ� GetPieceMovePosition

            //            Dictionary<(int Y, int X), (int Y, int X)> allowed_position = GetPieceMovePositionDict(slot.X, slot.Y, slot.GetPiece().pieceID);

            //            //�����⹰�� �̵������� ���� �� ������ġ�� �ִٸ� ��Ʃ�� ���긦 �����Ѵ�
            //            if (allowed_position.ContainsKey((targetSlotScript.Y, targetSlotScript.X)) == true)
            //            {
            //                return EGameState.StupidAction;
            //            }

            //        }
            //    }
            //}

            //���� �⹰�� ������ ���ڿ��� �屺�� �ҷ��°�?
            if (CheckRangeOpponentLion(targetSlotScript) == true)
            {
                result = EGameState.ThreatLion;
            }


            //���� � �׼��� ���ߴµ�, ������ �屺������ ���, ��Ʃ�;׼��� �ѰŴ�.
            foreach (BoardSlot thisSlot in env.BoardSlots)
            {
                //��������� 
                if (thisSlot.GetPiece() == null)
                {
                    continue;
                }

                //���� �⹰�̸�  
                if (thisSlot.GetPiece().eColor != eCurrentTurn)
                {
                    continue;
                }

                if (thisSlot.GetPiece().pieceID == Environment.L1 || thisSlot.GetPiece().pieceID == Environment.L2)
                {
                    BoardSlot myLioninSlot = thisSlot;

                    //���� �⹰������, thisSlot�� �� �� �ִ� ģ��?
                    foreach (BoardSlot enemySlot in env.BoardSlots)
                    {
                        if (enemySlot.GetPiece() == null)
                        {
                            continue;
                        }

                        if (enemySlot.GetPiece().eColor == eCurrentTurn)
                        {
                            continue;
                        }

                        var allowedPositionDict = GetPieceMovePositionDict(enemySlot.X, enemySlot.Y, enemySlot.GetPiece().pieceID);

                        if (allowedPositionDict.ContainsKey((myLioninSlot.Y, myLioninSlot.X)) == true)
                        {
                            return EGameState.StupidAction;
                        }

                    }


                }

            }

            return result;


        }

        public void SetReward(EGameState actionResult)
        {
            SharedDataType.EColor eOpponent = SharedDataType.EColor.Count;
            if (eCurrentTurn == SharedDataType.EColor.White)
            {
                eOpponent = SharedDataType.EColor.Black;
            }
            else if (eCurrentTurn == SharedDataType.EColor.Black)
            {
                eOpponent = SharedDataType.EColor.White;
            }


            switch (actionResult)
            {
                case EGameState.Promotion:
                    {
                        //UnityEngine.Debug.Log("��ȭ");
                        //Agent[eCurrentTurn].AddReward(+0.01f);
                        //Agent[eOpponent].AddReward(-0.01f);
                        break;
                    }
                case EGameState.OnBoardGiraph:
                    {
                        //UnityEngine.Debug.Log("�⸰ ����");
                        //Agent[eCurrentTurn].AddReward(+0.02f);
                        //Agent[eOpponent].AddReward(-0.01f);
                        break;
                    }
                case EGameState.OnBoardElephant:
                    {

                        //UnityEngine.Debug.Log("�ڳ��� ����");

                        //Agent[eCurrentTurn].AddReward(+0.015f);
                        //Agent[eOpponent].AddReward(-0.02f);
                        break;
                    }
                case EGameState.OnBoardChick:
                    {
                        //UnityEngine.Debug.Log("���Ƹ� ����");
                        //Agent[eCurrentTurn].AddReward(+0.01f);
                        break;
                    }
                case EGameState.CaptureChick:
                    {
                        //UnityEngine.Debug.Log("���Ƹ� ���");
                        //Agent[eCurrentTurn].AddReward(+0.01f);
                        //Agent[eOpponent].AddReward(-0.01f);
                        break;
                    }
                case EGameState.CaptureChicken:
                    {
                        //UnityEngine.Debug.Log("�� ���");
                        //Agent[eCurrentTurn].AddReward(+0.03f);
                        //Agent[eOpponent].AddReward(-0.03f);
                        break;
                    }
                case EGameState.CaptureElephant:
                    {
                        //UnityEngine.Debug.Log("�ڳ��� ���");

                        //Agent[eCurrentTurn].AddReward(+0.02f);
                        //Agent[eOpponent].AddReward(-0.02f);
                        break;
                    }
                case EGameState.CaptureGiraph:
                    {
                        //UnityEngine.Debug.Log("�⸰ ���");

                        //Agent[eCurrentTurn].AddReward(+0.03f);
                        //Agent[eOpponent].AddReward(-0.03f);
                        break;
                    }
                case EGameState.Win:
                    {
                        //UnityEngine.Debug.Log("State : Win");

                        Agent[eCurrentTurn].SetReward(+1.0f);
                        Agent[eOpponent].SetReward(-1.0f);



                        break;
                    }
                case EGameState.ThreatLion:
                    {
                        //UnityEngine.Debug.Log("���� ����");

                        //Agent[eCurrentTurn].AddReward(+0.01f);
                        //Agent[eOpponent].AddReward(-0.01f);
                        break;
                    }
                case EGameState.Continue:
                    {
                        //�Ϲ�
                        //UnityEngine.Debug.Log("�Ϲ� ������");
                        //Agent[eCurrentTurn].AddReward(-1.001f);
                        //Agent[eCurrentTurn].AddReward(-0.001f);
                        //Agent[eOpponent].AddReward(0f);
                        break;
                    }
                case EGameState.StupidAction:
                    {
                        //UnityEngine.Debug.Log("�����");
                        //Agent[eCurrentTurn].AddReward(-0.1f);
                        //Agent[eOpponent].AddReward(+0.1f);

                        //Agent[eCurrentTurn].EndEpisode();
                        //Agent[eOpponent].EndEpisode();

                        break;
                    }
                default:
                    {
                        //UnityEngine.Debug.Log("actionResult Warning");
                        break;
                    }
            }



        }

        public bool CheckTurn(SharedDataType.EColor color)
        {
            if (eCurrentTurn == color)
            {
                return true;
            }

            return false;
        }

        //Ư�� �⹰�� ��� ���� �� ó���ϴ� �޼ҵ�
        public void SelectSlot(BoardSlot slot)
        {
            //�⹰�� ��� �ִ� ���¶�� �����ϱ�
            //start_pos_str = GetStringFromPosition(slot.X, slot.Y);
            start_pos_str = GameManager.intPosToStringPos[(slot.Y, slot.X)];


            SelectedPiece = Instantiate(slot.GetPiece());
            SelectedPiece.transform.parent = transform;
            SelectedPiece.gameObject.SetActive(true);

            if (CheckTurn(SelectedPiece.eColor) == false)
            {
                //non select 
                return;
            }

            SelectedPiece.Selected();

            VisualizeMovePath(slot);


        }

        public void SelectStock(BoardStock selectedStock)
        {
            int pieceID = selectedStock.PieceID;

            GameObject createPieceObj = PieceManager.instance.CreatePiece(pieceID);
            SelectedPiece = createPieceObj.GetComponent<Piece>();
            SelectedPiece.gameObject.name = "SelectedStock";
            SelectedPiece.transform.parent = transform;
            SelectedPiece.Selected();

            switch (SelectedPiece.pieceID)
            {
                case Environment.E1:
                case Environment.E2:
                    {
                        start_pos_str = "E";
                        break;
                    }
                case Environment.G1:
                case Environment.G2:
                    {
                        start_pos_str = "G";
                        break;
                    }
                case Environment.P1:
                case Environment.P2:
                    {
                        start_pos_str = "P";
                        break;
                    }

            }

            VisualizeMovePath(SelectedPiece);


        }



        public (int, int) GetStockFromString(string stock_str)
        {
            int pieceID = 0;

            switch (stock_str)
            {
                case "P":
                case "C":
                    {
                        pieceID = 2;
                        break;
                    }
                case "E":
                    {
                        pieceID = 0;
                        break;
                    }
                case "G":
                    {
                        pieceID = 1;
                        break;
                    }
            }


            (int, int) stockID = ((int)eCurrentTurn, pieceID);


            return stockID;
        }


        //�����
        public void MoveTry(int target_x, int target_y)
        {
            if (start_pos_str == "")
            {
                return;
            }

            string target_pos_str = GameManager.intPosToStringPos[(target_y, target_x)];


            //���� �׼��� ���� ��ȯ�� �Ѵ� 
            double action = Decoder.encode_to_action_index(start_pos_str, target_pos_str, (double)eCurrentTurn);



            //�ش� �׼��� �������� üũ
            if (CheckAction(action) == false)
            {
                //����Ʈ �ǽ� �ʱ�ȭ
                UnSelectedPiece();
                return;
            }


            var results = SetActionMove(start_pos_str, target_pos_str);

            if (results == EGameState.Win)
            {
                GameManager.instance.MenuBoardObj.SetActive(true);
                ResetGame();
                return;
            }

            //����Ʈ �ǽ� �ʱ�ȭ
            UnSelectedPiece();

            ChangeTurn();

            return;

        }

        public delegate void DelegateChangeTurn(SharedDataType.EColor eColor);


        public DelegateChangeTurn delegateChange;



        public void ChangeTurn()
        {
            if (eCurrentTurn == SharedDataType.EColor.White)
            {
                eCurrentTurn = SharedDataType.EColor.Black;
            }
            else
            {
                eCurrentTurn = SharedDataType.EColor.White;
            }

            if(delegateChange != null)
            {
                delegateChange(eCurrentTurn);
            }
            

        }
        //public double GetToPlay(SharedDataType.EColor color)
        //{
        //    double to_play = -1;
        //    if (color == SharedDataType.EColor.White)
        //    {
        //        to_play = 1;
        //    }
        //    else
        //    {
        //        to_play = 0;
        //    }

        //    return to_play;
        //}


        public Dictionary<double, double> GetAvailableAllActions()
        {
            Dictionary<double, double> actions = new Dictionary<double, double>();

            List<BoardSlot> emptySlot = new List<BoardSlot>();

            //���忡 �ִ� �ǽ� ó��
            for (int y = 0; y < Environment.Y; ++y)
            {
                for (int x = 0; x < Environment.X; ++x)
                {
                    BoardSlot slot = env.BoardSlots[y, x].GetComponent<BoardSlot>();



                    Piece piece = slot.GetPiece();

                    //���Կ� �⹰�� ���� ��� �н�
                    if (piece == null)
                    {
                        emptySlot.Add(slot);
                        continue;
                    }

                    //�⹰�� ���� ���� �ϰ� �ٸ� ��� �н�
                    if (piece.eColor != eCurrentTurn)
                    {
                        continue;
                    }

                    //���� �⹰ �̵� �� �� �ִ� ��� ��Ʈ�� ���ڵ��ؼ� actions�� add
                    List<(int Y, int X)> allowed_position = GetPieceMovePosition(x, y, piece.pieceID);


                    //���� ��ġ���� �ش� �⹰�� �̵� ��θ� ��� ǥ��
                    for (int i = 0; i < allowed_position.Count; ++i)
                    {
                        //�� �� �ִ���
                        if (CheckMove(piece.eColor, allowed_position[i].X, allowed_position[i].Y) == false)
                        {
                            //�� �� ����
                            continue;
                        }
                        //�� �� �ִ� 

                        //start , end
                        //string start_pos = GetStringFromPosition(x, y);
                        string start_pos = GameManager.intPosToStringPos[(y, x)];

                        //string end_pos = GetStringFromPosition(allowed_position[i].X, allowed_position[i].Y);
                        string end_pos = GameManager.intPosToStringPos[(allowed_position[i].Y, allowed_position[i].X)];


                        double action = Decoder.encode_to_action_index(start_pos, end_pos, (double)eCurrentTurn);
                        actions.Add(action, action);
                    }

                }
            }


            int currentColor = (int)eCurrentTurn;



            for (int i = 0; i < 3; ++i)
            {
                if( env.BoardStocks[currentColor, i].Count <= 0 )
                {
                    continue;
                }
                
                string start_pos = "";
                switch (i)
                {
                    //�ڳ���
                    case 0:
                        {
                            start_pos = "E";
                            break;
                        }
                    //�⸰
                    case 1:
                        {
                            start_pos = "G";
                            break;
                        }
                    case 2:
                        {
                            start_pos = "P";
                            break;
                        }
                }

                foreach (BoardSlot slot in emptySlot)
                {
                    //string end_pos = GetStringFromPosition(slot.X, slot.Y);
                    string end_pos = GameManager.intPosToStringPos[(slot.Y, slot.X)];

                    double action = Decoder.encode_to_action_index(start_pos, end_pos, (double)eCurrentTurn);
                    actions.Add(action, action);
                }
            }
            return actions;
        }

        //��� �κ��� ��Ӱ�
        public void VisualizeAllNonPermission()
        {
            for (int y = 0; y < Environment.Y; ++y)
            {
                for (int x = 0; x < Environment.X; ++x)
                {
                    env.BoardSlots[y, x].SetPermission(true);
                }
            }
        }

        //Ư�� �⹰�� ������ �� �ִ� ��� ��ǥ ���� �Լ�
        public List<(int Y, int X)> GetPieceMovePosition(int start_x, int start_y, int pieceID)
        {
            (int dirY, int dirX)[] allowed_moves = ALLOWED_MOVES[pieceID];


            //���� ��ġ���� �̵� �� �� �ִ� ��ǥ�� ������ 
            List<(int Y, int X)> allowed_position = new List<(int Y, int X)>();

            for (int i = 0; i < allowed_moves.Length; ++i)
            {
                int moved_x_pos = start_x + allowed_moves[i].dirX;

                //x ��ǥ ��ȿ
                if (moved_x_pos < 0 || moved_x_pos >= Environment.X)
                    continue;

                int moved_y_pos = start_y + allowed_moves[i].dirY;

                //y��ǥ ��ȿ
                if (moved_y_pos < 0 || moved_y_pos >= Environment.Y)
                    continue;

                allowed_position.Add((moved_y_pos, moved_x_pos));
            }

            return allowed_position;
        }


        public Dictionary<(int Y, int X), (int Y, int X)> GetPieceMovePositionDict(int start_x, int start_y, int pieceID)
        {
            (int dirY, int dirX)[] allowed_moves = ALLOWED_MOVES[pieceID];


            //���� ��ġ���� �̵� �� �� �ִ� ��ǥ�� ������ 
            Dictionary<(int Y, int X), (int Y, int X)> allowed_position = new Dictionary<(int Y, int X), (int Y, int X)>();

            for (int i = 0; i < allowed_moves.Length; ++i)
            {
                int moved_x_pos = start_x + allowed_moves[i].dirX;

                //x ��ǥ ��ȿ
                if (moved_x_pos < 0 || moved_x_pos >= Environment.X)
                    continue;

                int moved_y_pos = start_y + allowed_moves[i].dirY;

                //y��ǥ ��ȿ
                if (moved_y_pos < 0 || moved_y_pos >= Environment.Y)
                    continue;

                allowed_position[(moved_y_pos, moved_x_pos)] = (moved_y_pos, moved_x_pos);
            }

            return allowed_position;
        }


        public void VisualizeMovePath(Piece selectedStockPiece)
        {
            VisualizeAllNonPermission();



            for (int y = 0; y < Environment.Y; ++y)
            {
                for (int x = 0; x < Environment.X; ++x)
                {
                    BoardSlot slot = env.BoardSlots[y, x].GetComponent<BoardSlot>();

                    if (slot.GetPiece() == null)
                    {
                        env.BoardSlots[y, x].SetPermission(false);
                    }

                }
            }



        }

        public bool CheckMove(SharedDataType.EColor pieceColor, int targetPos_x, int targetPos_y)
        {
            BoardSlot targetSlotScript = env.BoardSlots[targetPos_y, targetPos_x];

            if (targetSlotScript.GetPiece() == null)
            {
                return true;
            }

            Piece target_piece = targetSlotScript.GetPiece();

            SharedDataType.EColor targetColor = target_piece.eColor;

            if (pieceColor == targetColor)
            {
                //�Ʊ�
                return false;
            }
            else
            {
                return true;
            }
        }


        //�ش� ������ �ǽ��� �����Ÿ����� ���� ���̾��� �ִ��� 
        public bool CheckRangeOpponentLion(BoardSlot slot)
        {
            int pieceID = slot.GetPiece().pieceID;
            SharedDataType.EColor color = slot.GetPiece().eColor;
            int board_x = slot.X;
            int board_y = slot.Y;

            //���� ��ġ���� �̵� �� �� �ִ� ��ǥ�� ������ 
            List<(int Y, int X)> allowed_position = GetPieceMovePosition(board_x, board_y, pieceID);


            //���� ��ġ���� �ش� �⹰�� �̵� ��θ� ��� ǥ��
            for (int i = 0; i < allowed_position.Count; ++i)
            {
                //�� �� �ִ���
                if (CheckMove(color, allowed_position[i].X, allowed_position[i].Y) == false)
                {
                    continue;
                }

                //�ǽ��� �ִ°�?
                BoardSlot targetSlotScript = env.BoardSlots[allowed_position[i].Y, allowed_position[i].X];
                if (targetSlotScript.GetPiece() == null)
                {
                    continue;
                }


                if (targetSlotScript.GetPiece().pieceID == Environment.L1 || targetSlotScript.GetPiece().pieceID == Environment.L2)
                {
                    return true;
                }


            }


            return false;
        }

        //Ư�� �⹰�� ����� �� �̵� ���� ��� �����ֱ�
        public void VisualizeMovePath(BoardSlot slot)
        {
            int pieceID = slot.GetPiece().pieceID;
            SharedDataType.EColor color = slot.GetPiece().eColor;
            int board_x = slot.X;
            int board_y = slot.Y;

            //�������κ��� ��Ӱ�
            VisualizeAllNonPermission();


            //�ش� �⹰�� �̵� �� �� �ִ� �𷺼� ������ 

            //���� ��ġ���� �̵� �� �� �ִ� ��ǥ�� ������ 
            List<(int Y, int X)> allowed_position = GetPieceMovePosition(board_x, board_y, pieceID);


            //���� ��ġ���� �ش� �⹰�� �̵� ��θ� ��� ǥ��
            for (int i = 0; i < allowed_position.Count; ++i)
            {
                //�� �� �ִ���
                if (CheckMove(color, allowed_position[i].X, allowed_position[i].Y) == false)
                {
                    env.BoardSlots[allowed_position[i].Y, allowed_position[i].X].SetPermission(true);
                    continue;
                }

                env.BoardSlots[allowed_position[i].Y, allowed_position[i].X].SetPermission(false);

            }


            //���� ���� �⹰ ��ġ�� ��� ǥ��
            env.BoardSlots[board_y, board_x].SetPermission(false);


        }


        public void UnSelectedPiece()
        {
            start_pos_str = "";

            if (SelectedPiece != null)
            {

                SelectedPiece.CancleSelected();
                SelectedPiece.transform.localPosition = Vector3.zero;

            }
        }

        public void DestorySelectedPiece()
        {
            if (SelectedPiece == null)
            {
                return;
            }

            //start_pos_str = "";
            Destroy(SelectedPiece.gameObject);
            SelectedPiece = null;

        }

        public bool InactiveVisualizeMovePath()
        {
            bool result = false;

            for (int y = 0; y < Environment.Y; ++y)
            {
                for (int x = 0; x < Environment.X; ++x)
                {
                    env.BoardSlots[y, x].SetPermission(false);
                }
            }

            return result;
        }


    }

}