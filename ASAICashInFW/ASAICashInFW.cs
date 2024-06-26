namespace ProTopas.Impl.ASAICashInFW
{
    using System;
    using System.Runtime.InteropServices;
    using ProTopas.Diagnostics;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using Proxy.DataDictionary;
    using System.Text;
    using ProTopas.Impl.ASAIHelpers;
    using Microsoft.Win32;
    using ASAILOFW;
    using System.Threading;
    using ProTopas.Proxy.Cdm;
    using ProTopas.Proxy.CoinOut;
    using System.Data.SqlClient;
    using ProTopas.Proxy.SopDialog;
    using ProTopas.Proxy.ReceiptPrinter;
    using ProTopas.Proxy.Journal;
    using ProTopas.Proxy.Application;
    using Proxy.Sel;
    using MEI_BV_DLL;
    using MPOST;
    using System.ServiceModel;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using ASAI.TicketRedemptionFactory;
    using System.Net.Sockets;
    using System.Text.RegularExpressions;

    /*
        ASAICASHINFW: FW used for BV handling exclusively
        Changes History:
                    1. First Baseline.  Jan 16 2020 .Eduardo Toledo, Fernando Rojas and DM Herrera
                        POssibble BUgs| 
                            This FW  has a bottleneck on UpdateJSON. It must be removed asap to avoid collisions.
                            THIS FW is not reading counters from CDM to validate the status of dispensing.
                            Charity should be taken away from this FW. Next Modification.
                     2. Alle's Library (Author of BV Library) did many modifications. This FW now suporrts those modifications. Jan 18 2020. ET, DMH , FR
                     3. Including MAX VALUE OF TICKETS PER SESSION .Jan 20 2020. ET, DMH , FR
                     4. Issue on Prod. HP commened an OOS and the bill was in scrow. Now, we will call GetSTatus. ET, FR Jan 27 2021.
                     5. Add  Counters Posting to LO. ET Feb 3 2021
                     6. Revision of a case in redlands . ET and FR Feb 9 2021.
                     7. Adding Counters Posting wheh the BVs stack. ET and FR Feb 16 2021.
                     8. Reverting Back to version Feb 17 and adding controls for Looping when BVs are unplugged. ET March 10 2021
                     9. Getting statusses to BVS in order to avoid making unneeded calls to BV Library. ET March 11 2021. 
                     10. Correcting a case in Bogota when one bill and ticket are put simultaneoulsy. This occured when we command "GETSTATUS".
                        Based on Andres Lopez, this GetStatus should be called carefully before ENABLEACCEPTABLE only. ET March 11 2021.
                     11. There was a case of #Colissions in Bogota. Decoupling UpdateJSON to build TRansaction based on the list of Tickets stored on L
                        Left and Right March 12 ET. 
                    12. Modifying DisplayTickets . FR March 16 2021
                    13. Adding validations of dispenser counters. ET March 23 2021.
                    14. A new case in which the first ticket cannot be dispensed and then the flow goes to Multiple Tickets. (It was corrected ) ET March 26 2021.
                    15. Removing Timer for Tunring ON. FR, ET, DMH April 1 2021.
                    16. Add MinimizeShortPays in order to valdiate or not the counters of the dispenser. ET . DMH Aprilt 12 2021
                    17. Adding EVENT_BB_FAILED to support when the event os Stacking reports a fault and the event Cheatng is troggered from BB. FR, ET, DMH, April 30 2021
                    18. Adding Timer to protct the app from any reboot when the event of Stacking is nit triggered. ET, FR, DMH, May 02 2021
                    19. Read the EnableCIM1 and EnableCIM2 properties
                    20. Extra control when MEI Library does not trigger stack event.Fr, ET, Gm. July 15 2021
                    21. Control for Read MAxTicketRedemptionAmount and default to 3000. ET, GM, FR. Oct 11 2021.
                    22. A cheating event was recieved and we reoved the last ticket from the list LeftTickets and RightTikcets, respectively. ET, GM, FR Nov 09 2021
                    23. Improving readability of journals when a ticket is commnaded to stack ET, GM Dec 13 2021
                    24. Control for Read MAxTicketRedemptionCount and default to 10. ET, GM, FR. Feb 10 2022.
                    25. Adding version number in journals. ET, GM May 10 2022
                    26. Adding extracontrol for when the BV returns a ticke inmediatelly after a Scrow event. FR, ET, GM June 03 2022
                    27. When the ticket cannot be validated, the app returns it and there is a lack of synchro. The method OnReturnedLeft and onreturnedright 
                        was changed. June 15 2922. FR/GM/ET
                    28. Added option for Check Host Connection and SMS Connection and go to OOS, ATM only or 
                        Ticketing Only. Feb 21 2023. FR/GM/ET
                    29. Change For HOST and SMS not anymonore on connect and need call 
                        ASAICASHINFW_FUNC_SMS_HOST_STATUS = 120 ET,GM Mar 07 2023
                    30. Change for SMS Host to detect more than 1 IP. May 24 2023 FR/ET/GM
                    31. Add extra tracing  when the event of Stacked is never triggered. Aug 28 2023 ET.
                    32. Change raise event from scrow to stack to avoid dispense button problem (Amount 0). Dec 19 2023 ET, GM, FR.
                    33. Modify checkStackingEventTmr in BB for only tracing and no making additional logic. Jan 16 2024 FR/ET
                    34. Supressing //List<string> _leftCounters_t_1 and List<string> _rightCounters_t_1; In theory, it should work to 
                        control the lack of the event : StackedEvent but it is caused a colision with ANdres Lopez DLL. May 8 2024 ET/GM
                    35. Adding steps and screens for Charity Confirm function. Jun 26 2024 HE
     */
    public class ASAICashInFW : CCFrameWorkImpl
    {
        #region General Properties
        public const string ASAICASHINFW_PROP_TRANSACTION = "ASAICASHIN_TRANSACTION";
        public const string ASAICASHINFW_PROP_LEFTTICKETS = "ASAICASHIN_LEFTTICKETS";
        public const string ASAICASHINFW_PROP_RIGHTTICKETS = "ASAICASHIN_RIGHTTICKETS";
        public const string ASAICASHINFW_VERSION = "26062024"; //ddMMyyyy
        public const string ASAIBILLACCEPTOR_PROP_RIGHTCOUNTER = "ASAIBILLACCEPTOR_RIGHTCOUNTER";
        public const string ASAIBILLACCEPTOR_PROP_LEFTCOUNTER = "ASAIBILLACCEPTOR_LEFTCOUNTER";

        public const string ASAICASHINFW_PROP_BALANCE = "ASAICASHIN_BALANCE";
        public const string ASAICASHINFW_PROP_TRANSACTION_HTML = "ASAICASHIN_TRANSACTION_HTML";
        public const string ASAICASHINFW_PROP_BB_PARTIAL = "ASAICASHIN_BB_PARTIAL";

        public const string ASAILOFW_STATUS_OK = "devstate_ok";
        public const string ASAILOFW_STATUS_ERROR = "devstate_operator_needed";

        ///PROPERTIES
        public const short FUNC_GET_PROPERTY_STRING = 16;
        public const short FUNC_SET_PROPERTY_STRING = 17;

        public decimal MAX_TICKETSTRANSACTION_VALUE = 3000M;
        public int MAX_TICKETSTRANSACTION_COUNT = 10;
        public const int MSG500001 = 500001;
        #endregion general

        #region BV
        /*Functions to process Bill Validator */
        public const short ASAICASHIFW_FUNC_DISABLE = 2;
        public const short ASAICASHIFW_FUNC_DOCASHIN = 4;
        public const short ASAICASHIFW_FUNC_STACK = 5;
        public const short ASAICASHIFW_FUNC_REJECT = 6;
        public const short ASAICASHIFW_FUNC_STARTTRANSACTION = 7;
        
        public const short ASAICASHIFW_FUNC_MULTIPLES_TICKETS = 201;
        public const short ASAICASHIFW_FUNC_REJECT_NOTEBB = 202;
        public const short ASAICASHIFW_FUNC_CONNECT = 203;
        public const short ASAICASHIFW_FUNC_CONSOLIDATETICKETTRX = 204;

        /*Events to raise Bill Validator*/        
        public const short ASAICASHIFW_EVT_STACKED = 102;      
        public const short ASAICASHIFW_EVT_COMPLEX_GATEWAY_EXIT = 200;
        public const short ASAICASHIFW_EVT_BB_FAILED = 210;


        public const short ASAICASHIFW_FUNC_GETSTATUSLEFT = 12;
        public const short ASAICASHIFW_FUNC_RESETLEFT = 13;
        public const short ASAICASHIFW_FUNC_CLEARLEFT = 14;
        public const short ASAICASHIFW_FUNC_COUNTERSLEFT = 15;
        public const short ASAICASHIFW_FUNC_GETBVINFOLEFT = 18;

        public const short ASAICASHIFW_FUNC_GETSTATUSRIGHT = 31;
        public const short ASAICASHIFW_FUNC_RESETRIGHT = 32;
        public const short ASAICASHIFW_FUNC_CLEARRIGHT = 33;
        public const short ASAICASHIFW_FUNC_COUNTERSRIGHT = 34;
        public const short ASAICASHIFW_FUNC_GETBVINFORIGHT = 35;
        public const short ASAICASHINFW_FUNC_SMS_HOST_STATUS = 120;

        #region OOS and InService
        public const short ASAICASHIFW_FUNC_OOS = 62;
        public const short ASAICASHIFW_FUNC_INSERVICE = 63;
        #endregion

        #endregion BV

        #region Charity
        public const short ASAICASHIFW_FUNC_CHARITY = 65;
        public const short ASAICASHIFW_FUNC_CHARITYLIST = 66;
        public const short ASAICASHINFW_FUNC_CHARITYPROCESS = 67;
        public const short ASAICASHINFW_FUNC_CHARITYPROCESSSELECT = 69;
        public const short ASAICASHINFW_FUNC_CHARITYCONFIRM = 70;
        public const int MSG400023 = 400023;

        #endregion Charity

        #region Fields        
        public CCTrcErr trc;
        public CCDataDictionaryFW dataDict = new CCDataDictionaryFW();
        CCJournalFW journal = new CCJournalFW();
        MEI_BV_API api = new MEI_BV_API();
        ASAILOFW liveOffice = new ASAILOFW("");

        private CCCdmFW cdm = new CCCdmFW();
        private CCCoinOutFW coin = new CCCoinOutFW();

        public BVCounters counters;
        public int intHighAcceptorThreshold = 0;
        public bool enableCIM1 = true;
        public bool enableCIM2 = true;
     
        public bool isOOS = false;
        private bool getOutOfWelcome = false;
        private bool getOutOfMultipleTickets = false;
        private bool leftBVIsEnabled = false;
        private bool rightBVIsEnabled = false;
        //private bool commandingEnable = false;
        private bool BillOnEscrow = false;
        private bool minimizeShortPay = true;

        //private object to store ticket code, value;
        private List<KeyValuePair<string, decimal>> lefttickets = new List<KeyValuePair<string, decimal>>();
        private List<KeyValuePair<string, decimal>> righttickets = new List<KeyValuePair<string, decimal>>();
        private List<KeyValuePair<string, bool>> leftticketsStatus = new List<KeyValuePair<string, bool>>();
        private List<KeyValuePair<string, bool>> rightticketsStatus = new List<KeyValuePair<string, bool>>();
        //List<string> _leftCounters_t_1;
        //List<string> _rightCounters_t_1;

        public const string regeditAddress = @"SOFTWARE\\WOW6432Node\\Wincor Nixdorf\\ProTopas\\CurrentVersion\\CCSopStep\\DIALOG\\ENGLISH\\";
        #endregion

        enum BVConf {
            Left = 1,
            Right = 2
        }

        int numberOfTriestoConnect = 0;

        System.Timers.Timer checkStackingEventTmr;
        #region constructorsASAICashInFW
        public ASAICashInFW(string strName)
            : base(strName)
        {
            try
            {
                api.meiCmdLeft.Connecting += OnConnectingLeft;
                api.meiCmdLeft.Disabled += OnDisabledLeft;
                api.meiCmdLeft.Disconnecting += OnDisconnectingLeft;
                api.meiCmdLeft.Enabled += OnEnabledLeft;
                api.meiCmdLeft.Returning += OnReturningLeft;
                api.meiCmdLeft.Resetting += OnResettingLeft;
                api.meiCmdLeft.Stacking += OnStackingLeft;

                api.meiLeft.Stacked += OnStackedLeft;
                api.meiLeft.Returned += OnReturnedLeft;
                api.meiLeft.CashBoxRemoved += OnCashBoxRemovedLeft;
                api.meiLeft.CashBoxAttached += OnCashBoxAttachedLeft;
                api.meiLeft.StackerFull += OnStackerFullLeft;
                api.meiLeft.StackerFullCleared += OnStackerFullClearedLeft;
                api.meiLeft.Rejected += OnRejectedLeft;
                api.meiLeft.Escrow += OnEscrowLeft;
                api.meiLeft.JamDetected += OnJamDetectedLeft;
                api.meiLeft.JamCleared += OnJamClearedLeft;
                api.meiLeft.NoteRetrieved += OnNoteRetrievedLeft;               //  This event is not posted with this MEI model
                api.meiLeft.FailureDetected += OnFailureDetectedLeft;
                api.meiLeft.FailureCleared += OnFailureClearedLeft;
                api.meiLeft.Connected += OnConnectedLeft;
                api.meiLeft.Disconnected += OnDisconnectedLeft;
                api.meiLeft.PowerUp += OnPowerUpLeft;
                api.meiLeft.PowerUpComplete += OnPowerUpCompleteLeft;
                api.meiLeft.PUPEscrow += OnPUPEscrowLeft;
                api.meiLeft.Cheated += OnCheatedLeft;

                api.meiCmdRight.Connecting += OnConnectingRight;
                api.meiCmdRight.Disabled += OnDisabledRight;
                api.meiCmdRight.Disconnecting += OnDisconnectingRight;
                api.meiCmdRight.Enabled += OnEnabledRight;
                api.meiCmdRight.Returning += OnReturningRight;
                api.meiCmdRight.Resetting += OnResettingRight;
                api.meiCmdRight.Stacking += OnStackingRight;

                api.meiRight.Stacked += OnStackedRight;
                api.meiRight.Returned += OnReturnedRight;
                api.meiRight.CashBoxRemoved += OnCashBoxRemovedRight;
                api.meiRight.CashBoxAttached += OnCashBoxAttachedRight;
                api.meiRight.StackerFull += OnStackerFullRight;
                api.meiRight.StackerFullCleared += OnStackerFullClearedRight;
                api.meiRight.Rejected += OnRejectedRight;
                api.meiRight.Escrow += OnEscrowRight;
                api.meiRight.JamDetected += OnJamDetectedRight;
                api.meiRight.JamCleared += OnJamClearedRight;
                api.meiRight.NoteRetrieved += OnNoteRetrievedRight;              //  This event is not posted with this MEI model
                api.meiRight.FailureDetected += OnFailureDetectedRight;
                api.meiRight.FailureCleared += OnFailureClearedRight;
                api.meiRight.Connected += OnConnectedRight;
                api.meiRight.Disconnected += OnDisconnectedRight;
                api.meiRight.PowerUp += OnPowerUpRight;
                api.meiRight.PowerUpComplete += OnPowerUpCompleteRight;
                api.meiRight.PUPEscrow += OnPUPEscrowRight;
                api.meiRight.Cheated += OnCheatedRight;

                checkStackingEventTmr = new System.Timers.Timer(60000);
                checkStackingEventTmr.Elapsed += CheckStackingEventTmr_Elapsed;
                checkStackingEventTmr.Stop();

                numberOfTriestoConnect = 0; // When the app rises up by frist time , this value is set in zero
                minimizeShortPay =false;
                //ReadMinimizeShortPay();
                //PrimaryHostIsConnected();
            }
            catch (Exception ex)
            {
                WriteJournal(string.Format("ConstructorsASAICashInFW - Exception {0}, {1}", ex.Message, ex.StackTrace));
            }
        }

        private void CheckStackingEventTmr_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                WriteJournal($"CheckStackingEventTmr_Elapsed: Stacked Event was never triggered");
                checkStackingEventTmr.Stop();

                foreach (var item in api.meiCmdLeft.GetCounters())
                    WriteJournal($"CheckStackingEventTmr_Elapsed: Current MEICMD.Left Counter {item}");
                foreach (var item in api.meiCmdRight.GetCounters())
                    WriteJournal($"CheckStackingEventTmr_Elapsed: Current MEICMD.Right Counter {item}");
           
                //Biloxy has been reporting casses in which BB is not working. Jan 16 2024
                /*if (_leftCounters_t_1 != null)
                    foreach (var item in _leftCounters_t_1)
                        WriteJournal($"CheckStackingEventTmr_Elapsed: MEICMD.Left Counter  en T-1 {item}");
                if (_rightCounters_t_1 != null)
                    foreach (var item in _rightCounters_t_1)
                        WriteJournal($"CheckStackingEventTmr_Elapsed:  MEICMD.Right Counter  en T-1 {item}");
            
                PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR, ASAILOFW_STATUS_ERROR);
                PostCounters();

                FrmSendEvent(ASAICASHIFW_EVT_BB_FAILED);
                */
            }
            catch (Exception ex)
            {
                WriteJournal($"CheckStackingEventTmr_Elapsed Exception {ex.ToString()}");
            }

        }

        /* private void TurnOnTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
         {

             string statusLeft = api.meiCmdLeft.GetStatus().deviceArgs.state;
             if (statusLeft.ToUpper() == "ESCROW")
                 api.meiCmdLeft.EscrowReturn();
             string statusRight = api.meiCmdRight.GetStatus().deviceArgs.state;
             if (statusRight.ToUpper() == "ESCROW")
                 api.meiCmdRight.EscrowReturn();
             api.meiCmdLeft.EnableAcceptance(true);
             api.meiCmdRight.EnableAcceptance(true);

             WriteJournal($"ASAICashInFW - TurnOnTimer_Elapse statusLeft: {statusLeft} rightLeft: {statusRight}");
             turnOnTimer.Stop();
         }*/

        private void OnReturningLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal($"OnReturningLeft: command {e.commandArgs.command} ResultCode= {e.commandArgs.resultCode} Text: {e.commandArgs.resultText} ");
                    break;
            }
         }

        private void OnReturningRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Right:
                    WriteJournal($"OnReturningRight: command {e.commandArgs.command} ResultCode= {e.commandArgs.resultCode} Text: {e.commandArgs.resultText} ");
                    break;
            }
        }

       
        private void OnResettingRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Right:
                    WriteJournal($"OnRessetingRight: command {e.commandArgs.command} ResultCode= {e.commandArgs.resultCode} Text: {e.commandArgs.resultText} ");
                    break;
            }
        }

        private void OnResettingLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal($"OnStackingLeft: command {e.commandArgs.command} ResultCode= {e.commandArgs.resultCode} Text: {e.commandArgs.resultText} ");
                    break;
            }
        }

        private void OnStackingLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal($"OnStackingLeft: command {e.commandArgs.command} ResultCode= {e.commandArgs.resultCode} Text: {e.commandArgs.resultText} ");
                    switch (e.commandArgs.resultCode)
                    {
                        case BVCommandResultCode.CommandSuccess:
                            
                            break;                        
                        default:
                            Transaction transaction = GetTransactionFromDataDict();
                            if (transaction != null)
                            {
                                string ret = transaction.ToJSON();
                                WriteJournal((String.Format("OnCheatedLeft JSON {0}", ret)));
                                if (transaction.type == "TR")
                                {

                                    if (!getOutOfMultipleTickets)
                                    {
                                        getOutOfMultipleTickets = true;
                                        RaiseEvent();
                                    }
                                }
                                if (transaction.type == "BB")
                                {
                                    RaiseEvent2(ASAICASHIFW_EVT_BB_FAILED);
                                }
                            }
                            break;
                    }
                    break;
            }
        }

        private void OnStackingRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Right:
                    WriteJournal($"OnStackingRight: command {e.commandArgs.command} ResultCode= {e.commandArgs.resultCode} Text: {e.commandArgs.resultText} ");
                    switch (e.commandArgs.resultCode)
                    {
                        case BVCommandResultCode.CommandSuccess:

                            break;
                        default:
                            Transaction transaction = GetTransactionFromDataDict();
                            if (transaction != null)
                            {
                                string ret = transaction.ToJSON();
                                WriteJournal((String.Format("OnCheatedLeft JSON {0}", ret)));
                                if (transaction.type == "TR")
                                {

                                    if (!getOutOfMultipleTickets)
                                    {
                                        getOutOfMultipleTickets = true;
                                        RaiseEvent();
                                    }
                                }
                                if (transaction.type == "BB")
                                {
                                    RaiseEvent2(ASAICASHIFW_EVT_BB_FAILED);
                                }
                            }
                            break;
                    }
                    break;
            }
        }

        #endregion

        #region ProTopas interface
        public override short OnFrmRequest(short methodId, IntPtr data1, int dataLen1, IntPtr data2, int dataLen2, IntPtr data3, int dataLen3, IntPtr data4, int dataLen4, IntPtr data5, int dataLen5, uint misc)
        {
            short result = 0;
            string propertyName = "-";
            String ret = String.Empty;
            Transaction transaction = null;
            string statusLeft = String.Empty;
            string statusRight = String.Empty;

            try
            {
                if (FrmGetName().StartsWith(SegmentInfo.ASAIFramework))
                {
                    switch (methodId)
                    {
                        #region DISABLE
                        case ASAICASHIFW_FUNC_DISABLE:
                            WriteJournal("ASAICashInFW - START DISABLE_MEI");
                            string json = string.Empty;
                            dataDict.Get(ref json, ASAICASHINFW_PROP_TRANSACTION);
                            WriteJournal($"ASAICashInFW - START DISABLE_MEI json {json}");

                            

                            string ticketStatus = string.Empty;
                            ticketStatus = JsonConvert.SerializeObject(statusLeft);
                            WriteJournal($"ASAICashInFW - ASAICASHIFW_FUNC_DISABLE statusLeft  {ticketStatus}");

                            ticketStatus = string.Empty;
                            ticketStatus = JsonConvert.SerializeObject(statusRight);
                            WriteJournal($"ASAICashInFW - ASAICASHIFW_FUNC_DISABLE statusRight {ticketStatus}");

                            /*
                            // If there is  an ongoing transaction
                            if (leftticketsStatus.Count > 0)  {
                                //  there is  an  ongoing transaction in Left and thus, 
                                // pinpoint if there is ticket in proces for stacking
                                int index = leftticketsStatus.FindIndex(p => p.Value == false);
                                if (index != -1)
                                {
                                    //It is safe to send EscrowReturn and Disable.
                                    api.meiCmdLeft.EscrowReturn();
                                }
                      
                            }
                            else
                                //  there is  no ongoing transaction in Left and thus, it is safe to command EscrowReturn
                                api.meiCmdLeft.EscrowReturn();

                            // If there is  an ongoing transaction
                            if (rightticketsStatus.Count > 0)
                            {
                                //  there is  an  ongoing transaction in Left and thus, 
                                // pinpoint if there is ticket in proces for stacking
                                int index = rightticketsStatus.FindIndex(p => p.Value == false);
                                if (index != -1)
                                {
                                    //It is safe to send EscrowReturn and Disable.
                                    api.meiCmdRight.EscrowReturn();
                                }
                               
                            }
                            else
                                //  there is  no ongoing transaction in Left and thus, it is safe to command EscrowReturn
                                api.meiCmdRight.EscrowReturn();

                            */
                            api.meiCmdLeft.EscrowReturn();
                            api.meiCmdRight.EscrowReturn();
                            api.meiCmdLeft.EnableAcceptance(false);
                            api.meiCmdRight.EnableAcceptance(false);
                            result = CCFRMW_RC_OK;
                            WriteJournal("ASAICashInFW - END DISABLE_MEI");
                            break;
                            #endregion DISABLE

                        #region CONSOLIDATE TICKET TRANSACTION 
                            // This method is going to be used to remove the bottleneck of the first baseline
                            // This should be mapped into ASAICASHINMAINSTEP and create a new Setpe whoch should be 
                            //invocated after htting the dipense button to consolidate an unique Transaction/JSON knowing
                            //left tickets and right tickets are inmutable at that point of the transaction. Search by #Collisions 
                            //to uncomment the code
                        case ASAICASHIFW_FUNC_CONSOLIDATETICKETTRX:
                            WriteJournal("ASAICashInFW - START ASAICASHIFW_FUNC_CONSOLIDATETICKETTRX");
                            //Read LeftTicket from DataDictionary/new property and deserialize it
                            string tickets = string.Empty;
                            dataDict.Get(ref tickets, ASAICASHINFW_PROP_LEFTTICKETS);
                            lefttickets = JsonConvert.DeserializeObject<List<KeyValuePair<string, decimal>>>(tickets);
                            WriteJournal($"ASAICashInFW - END ASAICASHIFW_FUNC_CONSOLIDATETICKETTRX LeftTickets  {tickets}");
                            //Read RightTicket from DataDictionary/new property and deserialize it
                            tickets = string.Empty;
                            dataDict.Get(ref tickets, ASAICASHINFW_PROP_RIGHTTICKETS);
                            righttickets = JsonConvert.DeserializeObject<List<KeyValuePair<string, decimal>>>(tickets);
                            WriteJournal($"ASAICashInFW - END ASAICASHIFW_FUNC_CONSOLIDATETICKETTRX RightTickets  {tickets}");
                            ConsolidateTicketsTransaction(lefttickets,righttickets);
                            WriteJournal("ASAICashInFW - END ASAICASHIFW_FUNC_CONSOLIDATETICKETTRX");
                            result = CCFRMW_RC_OK;
                            break;
                        #endregion STARTTRANSACTION

                        #region CONNECT
                        case ASAICASHIFW_FUNC_CONNECT:
                            WriteJournal("ASAICashInFW - START ASAICASHIFW_FUNC_CONNECT");
                            if (numberOfTriestoConnect == 0)
                            {
                                WriteJournal("ASAICashInFW - START ASAICASHIFW_FUNC_CONNECT FIRST CONNECT ");
                                Task t = Task.Run(() =>
                                {
                                    Stopwatch s = new Stopwatch();
                                    s.Start();
                                    while (s.Elapsed < TimeSpan.FromSeconds(2))
                                    {
                                    }
                                    s.Stop();
                                });
                                t.Wait();
                                WriteJournal("ASAICashInFW - START ASAICASHIFW_FUNC_CONNECT EXITING OUT FROM WAITING 2 SECONDS");

                                ReadParameters();
                                WriteJournal($"ASAICashInFW - START ASAICASHIFW_FUNC_CONNECT EnableCIM1 {enableCIM1} EnableCIM12 {enableCIM2} MaxAmountTicketRedemtion {MAX_TICKETSTRANSACTION_VALUE}");

                            }
                            numberOfTriestoConnect++;
                            WriteJournal("ASAICashInFW - FUNC_CONNECT_Calling PostStatusBillDenomination");
                            PostCounters();
                            statusLeft = api.meiCmdLeft.GetStatus().deviceArgs.state;
                            statusRight = api.meiCmdRight.GetStatus().deviceArgs.state;
                            WriteJournal($"ASAICashInFW - FUNC_CONNECT_GETTING BV STATUSSES LEFT {statusLeft} RIGHT {statusRight}");

                            if (statusLeft.ToUpper() == "DISCONNECTED")
                                api.meiCmdLeft.Connect();
                            if (statusRight.ToUpper() == "DISCONNECTED")
                                api.meiCmdRight.Connect();
                            WriteJournal("ASAICashInFW - END ASAICASHIFW_FUNC_CONNECT");
                            break;
                        #endregion CONNECT

                        #region ASAICASHINFW_FUNC_SMS_HOST_STATUS
                        case ASAICASHINFW_FUNC_SMS_HOST_STATUS:
                            WriteJournal("ASAICashInFW - START ASAICASHINFW_FUNC_SMS_HOST_STATUS");

                            CCReceiptPrinterFW printer = new CCReceiptPrinterFW();
                            short pStatus = printer.GetbStatus();
                            WriteJournal($"ASAICashInFW - printer status : {pStatus}");
                            if (pStatus == CCReceiptPrinterFW.CCRECPRT_OPERATIONAL)
                            {
                                string oosShortpay = "1";
                                try
                                {
                                    dataDict.Get(ref oosShortpay, "PASSPORTOOSSHORTPAY");
                                }
                                catch (Exception ex)
                                {

                                }
                                if (oosShortpay == "0")
                                {
                                    result = (short)3;
                                    dataDict.Set("1", "PASSPORTTICKETSTATUS");
                                }
                                else
                                { 
                                    if (PrimaryHostIsConnected())
                                        if (StartUp())
                                        {
                                            result = CCFRMW_RC_OK;
                                            dataDict.Set("0", "PASSPORTTICKETSTATUS");
                                        }
                                        else
                                        {
                                            result = (short)2;
                                            dataDict.Set("1", "PASSPORTTICKETSTATUS");
                                        }
                                    else
                                    {
                                        if (StartUp())
                                        {
                                            result = (short)1;
                                            dataDict.Set("0", "PASSPORTTICKETSTATUS");

                                        }
                                        else
                                        {
                                            result = (short)3;
                                            dataDict.Set("1", "PASSPORTTICKETSTATUS");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                result = (short)3;
                                dataDict.Set("1", "PASSPORTTICKETSTATUS");
                            }

                            WriteJournal($"ASAICashInFW - END PrimaryHostIsConnected / StartUp Result: {result}");
                            WriteJournal("ASAICashInFW - END ASAICASHINFW_FUNC_SMS_HOST_STATUS");
                            break;
                        #endregion ASAICASHINFW_FUNC_SMS_HOST_STATUS
                        #region STARTTRANSACTION
                        case ASAICASHIFW_FUNC_STARTTRANSACTION:
                            WriteJournal("ASAICashInFW - START STARTTRANSACTION");
                            //Clean private object of tickets (Left and Right)
                            lefttickets.Clear();
                            righttickets.Clear();
                            leftticketsStatus.Clear();
                            rightticketsStatus.Clear();
                            WriteJournal(String.Format("ASAICashInFW - START STARTTRANSACTION LeftTickets.Count {0} RighTickets.Count {1} ", lefttickets.Count, righttickets.Count));
                            dataDict.Set(string.Empty, ASAICASHINFW_PROP_TRANSACTION);
                            dataDict.Set(string.Empty, ASAICASHINFW_PROP_TRANSACTION_HTML);
                            //Uncomment it to work without potential collisions
                            //dataDict.Set(string.Empty, ASAICASHINFW_PROP_LEFTTICKETS);
                            //dataDict.Set(string.Empty, ASAICASHINFW_PROP_RIGHTTICKETS);

                            BillOnEscrow = false;

                            getOutOfWelcome = false;
                            getOutOfMultipleTickets = false;

                            statusLeft = api.meiCmdLeft.GetStatus().deviceArgs.state;
                            if (statusLeft.ToUpper() == "ESCROW")
                                api.meiCmdLeft.EscrowReturn();
                            statusRight = api.meiCmdRight.GetStatus().deviceArgs.state;
                            if (statusRight.ToUpper() == "ESCROW")
                                api.meiCmdRight.EscrowReturn();

                            if(enableCIM1)
                            {
                                api.meiCmdLeft.EnableAcceptance(true);
                            }
                            if (enableCIM2)
                            {
                                api.meiCmdRight.EnableAcceptance(true);
                            }

                            WriteJournal($"ASAICashInFW - STARTTRANSACTION statusLeft: {statusLeft} rightLeft: {statusRight}");
                            WriteJournal($"ASAICashInFW - STARTTRANSACTION END");

                            result = CCFRMW_RC_OK;
                            break;
                        #endregion STARTTRANSACTION

                        #region MULTIPLES_TICKETS
                        case ASAICASHIFW_FUNC_MULTIPLES_TICKETS:
                            WriteJournal("ASAICashInFW - MULTIPLES_TICKETS");
                            getOutOfMultipleTickets = false;
                            break;
                        #endregion MULTIPLES_TICKETS

                        #region STACK NOTE BILL BREAKING
                        case ASAICASHIFW_FUNC_STACK:
                            WriteJournal("ASAICashInFW - ASAICASHIFW_FUNC_STACK START" );
                            ret = string.Empty;
                            dataDict.Get(ref ret, ASAICASHINFW_PROP_TRANSACTION);
                            WriteJournal(String.Format("ASAICashInFW - ASAICASHIFW_FUNC_STACK {0}", ret));
                            transaction = DeserializeJSON(ret);
                            if (transaction.items != null)
                            {
                                if (transaction.items.Count > 0)
                                { 
                                    WriteJournal(String.Format("ASAICashInFW -Calling Stack to BB to BV Side {0}", transaction.items[0].billAcceptor));
                                    checkStackingEventTmr.Start();
                                    //_leftCounters_t_1 = api.meiCmdLeft.GetCounters();
                                    //_rightCounters_t_1 = api.meiCmdRight.GetCounters();
                                    if (transaction.items[0].billAcceptor == (int)BVConf.Left) { 
                                        Task.Run(() => {
                                            api.meiCmdLeft.EscrowStack();
                                        });
                                        

                                    }
                                    else if (transaction.items[0].billAcceptor == (int)BVConf.Right)                 {
                                        Task.Run(() => {
                                            api.meiCmdRight.EscrowStack();
                                        });
                                    }
                                    WriteJournal($"ASAICashInFW - Stack command was sent to BV {transaction.items[0].billAcceptor}");

                                }
                                else
                                    WriteJournal(String.Format("ASAICashInFW -Calling Stack to BB but it has no items"));
                            }
                            else
                                WriteJournal(String.Format("ASAICashInFW -Calling Stack to BB but transaction.items == null"));
                            break;
                        #endregion

                        #region REJECT NOTE BILL BREAKING
                        case ASAICASHIFW_FUNC_REJECT_NOTEBB:
                            WriteJournal("ASAICashInFW - ASAICASHIFW_FUNC_REJECT_NOTEBB START");
                            ret = string.Empty;
                            dataDict.Get(ref ret, ASAICASHINFW_PROP_TRANSACTION);
                            WriteJournal(String.Format("ASAICashInFW - ASAICASHIFW_FUNC_REJECT_NOTEBB {0}", ret));
                            transaction = DeserializeJSON(ret);
                            if (transaction.items != null)
                            {
                                if (transaction.items.Count > 0)
                                {
                                    WriteJournal(String.Format("ASAICashInFW -Calling Return to BB to BV Side {0}", transaction.items[0].billAcceptor));
                                    if (transaction.items[0].billAcceptor == (int)BVConf.Left)
                                        api.meiCmdLeft.EscrowReturn();
                                    else if (transaction.items[0].billAcceptor == (int)BVConf.Right)
                                        api.meiCmdRight.EscrowReturn();
                                }
                                else
                                    WriteJournal(String.Format("ASAICashInFW -Calling Retrun to BB but it has no items"));
                            }
                            else
                                WriteJournal(String.Format("ASAICashInFW -Calling Return to BB but transaction.items == null"));
                            break;
                        #endregion
                        
                        #region Properties
                        case FUNC_GET_PROPERTY_STRING:                            
                            FrmFillObject(ref propertyName, data1, dataLen1);                            
                            break;
                        case FUNC_SET_PROPERTY_STRING:
                            FrmFillObject(ref propertyName, data1, dataLen1);                           
                            break;
                        #endregion Properties

                        #region SOP

                        case ASAICASHIFW_FUNC_RESETLEFT:
                            WriteJournal("ASAICashInFW - FUNC_RESETLEFT START");
                            result = ResetLeft();
                            WriteJournal("ASAICashInFW - FUNC_RESETLEFT END");
                            break;
                        case ASAICASHIFW_FUNC_CLEARLEFT:
                            WriteJournal("ASAICashInFW - FUNC_CLEARLEFT START");
                            //result = ClearLeft();                          
                            WriteJournal("ASAICashInFW - FUNC_CLEARLEFT END");
                            break;
                        case ASAICASHIFW_FUNC_COUNTERSLEFT:
                            WriteJournal("ASAICashInFW - FUNC_COUNTERSLEFT START");
                            result = CountersLeft();
                            WriteJournal("ASAICashInFW - FUNC_COUNTERSLEFT END");
                            break;
                        case ASAICASHIFW_FUNC_GETSTATUSLEFT:
                            WriteJournal("ASAICashInFW - FUNC_GETSTATUSLEFT START");
                            result = GetStatusLeft();
                            WriteJournal("ASAICashInFW - FUNC_GETSTATUSLEFT END");
                            journal.Write(500001);
                            break;
                        case ASAICASHIFW_FUNC_GETBVINFOLEFT:
                            WriteJournal("ASAICashInFW - START GETBVINFOLEFT SOP");
                            result = GetBvInfoLeft();
                            WriteJournal("ASAICashInFW - END GETBVINFOLEFT SOP");
                            break;

                        case ASAICASHIFW_FUNC_RESETRIGHT:
                            WriteJournal("ASAICashInFW - FUNC_RESETRIGHT START");
                            result = ResetRight();
                            WriteJournal("ASAICashInFW - FUNC_RESETRIGHT END");
                            break;
                        case ASAICASHIFW_FUNC_CLEARRIGHT:
                            WriteJournal("ASAICashInFW - FUNC_CLEARRIGHT START");
                            //result = ClearRight();
                            WriteJournal("ASAICashInFW - FUNC_CLEARRIGHT END");
                            break;
                        case ASAICASHIFW_FUNC_COUNTERSRIGHT:
                            WriteJournal("ASAICashInFW - FUNC_COUNTERSRIGH START");
                            result = CountersRight();
                            WriteJournal("ASAICashInFW - FUNC_COUNTERSRIGH END");
                            break;
                        case ASAICASHIFW_FUNC_GETSTATUSRIGHT:
                            WriteJournal("ASAICashInFW - FUNC_GETSTATUSRIGHT START");
                            result = GetStatusRight();
                            WriteJournal("ASAICashInFW - FUNC_GETSTATUSRIGHT END");
                            break;
                        case ASAICASHIFW_FUNC_GETBVINFORIGHT:
                            WriteJournal("ASAICashInFW - START GETBVINFORIGHT SOP");
                            result = GetBvInfosRight();
                            WriteJournal("ASAICashInFW - END GETBVINFORIGHT SOP");
                            break;

                        #endregion SOP

                        #region Charity
                        //DM it would have to be passed to other FW.
                        case ASAICASHIFW_FUNC_CHARITY:
                            dataDict.Set("CHARITY", "ASAICASHIN_STATE");
                            result = Charity();
                            break;
                        case ASAICASHIFW_FUNC_CHARITYLIST:
                            dataDict.Set("CHARITY", "ASAICASHIN_STATE");
                            result = CharityList();
                            break;
                        case ASAICASHINFW_FUNC_CHARITYPROCESSSELECT:
                            dataDict.Set("CHARITY", "ASAICASHIN_STATE");
                            result = CharityProcessSelect();
                            break;
                        case ASAICASHINFW_FUNC_CHARITYCONFIRM:
                            dataDict.Set("CHARITY", "ASAICASHIN_STATE");
                            result = CharityConfirm();
                            break;
                        case ASAICASHINFW_FUNC_CHARITYPROCESS:
                            dataDict.Set("CHARITY", "ASAICASHIN_STATE");
                            result = CharityProcess();
                            break;
                        #endregion

                        #region OOS and InService
                        case ASAICASHIFW_FUNC_OOS:                                        
                            WriteJournal("ASAICashInFW - START OOS");
                            //Thread proc = new Thread(new ThreadStart(OOS_ThreadSafe));
                            //proc.Start();

                            //result = OOS();

                            Task tOos = Task.Run(() =>
                            {
                                result = OOS();
                                WriteJournal("ASAICashInFW - END OOS");
                            });
                            //WriteJournal("ASAICashInFW - END OOS");                           
                            break;
                        case ASAICASHIFW_FUNC_INSERVICE:                                       
                            WriteJournal("ASAICashInFW - START INSERVICE");                            
                            result = InService();
                            WriteJournal("ASAICashInFW - END INSERVICE");                            
                            break;
                        #endregion

                        default:
                            dataDict.Set("DEFAULT", "ASAICASHIN_STATE");
                            result = CCFRMW_RC_FUNCTION_NOT_SUPPORTED;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                WriteJournal("ASAICashInFW - OnFrmRequest - Message: " + e.Message + " StackTrace: " + e.StackTrace);
            }
            return result;
        }

        public override short OnFrmEvent(string senderFrameWork, short eventId, IntPtr data, int dataLen)
        {
            short result = (short)ProTopas.Data.EventResult.Ok;

            try
            {
                result = base.OnFrmEvent(senderFrameWork, eventId, data, dataLen);
            }
            catch (Exception e)
            {
               
            }

          
            return result;
        }
        #endregion ProTopas interface

        #region MEI

        private void OnEnabledLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal($"OnEnabledLeft: command {e.commandArgs.command} ResultCode= {e.commandArgs.resultCode} Text: {e.commandArgs.resultText}  State = {e.deviceArgs.state}");
                    if (e.deviceArgs.state.ToUpper() == "ESCROW")
                    {
                        WriteJournal($"OnEnabledLeft:Returning after detecting a note into escrow");
                        api.meiCmdLeft.EscrowReturn();
                    }
                    switch (e.commandArgs.resultCode)
                    {
                        case BVCommandResultCode.CommandSuccess:
                            leftBVIsEnabled = true;
                            PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR, ASAILOFW_STATUS_OK);
                            break;
                        case BVCommandResultCode.NotConnectedUnavailable:
                            leftBVIsEnabled = false;
                            PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR, ASAILOFW_STATUS_ERROR);
                            break;
                        default:
                            leftBVIsEnabled = false;
                            PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR, ASAILOFW_STATUS_ERROR);
                            break;
                    }
                    break;
            }
            WriteJournal($"OnEnabledLeft: commandingEnable: leftBVIsEnabled:{leftBVIsEnabled} rightBVIsEnabled:{rightBVIsEnabled}");
            if (!leftBVIsEnabled && !rightBVIsEnabled)
            {
                Transaction transaction = new Transaction
                {
                    type = "ERROR"
                };
                string ret = transaction.ToJSON();
                dataDict.Set(ret, ASAICASHINFW_PROP_TRANSACTION);
                WriteJournal(string.Format("OnEnabledLeft  JSON: {0}", ret));
                //It wouuld allowto BASI_IDDLE LOOP to sanitize CR .
                Task t = Task.Run(() =>
                {
                    Stopwatch s = new Stopwatch();
                    s.Start();
                    while (s.Elapsed < TimeSpan.FromSeconds(1))
                    {

                    }
                    s.Stop();
                });
                t.Wait();
                RaiseEvent();
            }
        }

        private void OnEnabledRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Right:
                    WriteJournal($"OnEnabledRight: command {e.commandArgs.command} ResultCode= {e.commandArgs.resultCode} Text: {e.commandArgs.resultText}  State = {e.deviceArgs.state}");
                    if (e.deviceArgs.state.ToUpper() == "ESCROW")
                    {
                        WriteJournal($"OnEnabledRight:Returning after detecting a note into escrow");
                        api.meiCmdRight.EscrowReturn();
                    }
                    switch (e.commandArgs.resultCode)
                    {
                        case BVCommandResultCode.CommandSuccess:
                            rightBVIsEnabled = true;
                            PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR2, ASAILOFW_STATUS_OK);
                            break;
                        case BVCommandResultCode.NotConnectedUnavailable:
                            rightBVIsEnabled = false;
                            PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR2, ASAILOFW_STATUS_ERROR);
                            break;
                        default:
                            rightBVIsEnabled = false;
                            PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR2, ASAILOFW_STATUS_ERROR);
                            break;
                    }
                    break;
            }
            WriteJournal($"OnEnabledRight: commandingEnable: leftBVIsEnabled:{leftBVIsEnabled} rightBVIsEnabled:{rightBVIsEnabled}");
            if (!leftBVIsEnabled && !rightBVIsEnabled)
            {
                Transaction transaction = new Transaction
                {
                    type = "ERROR"
                };
                string ret = transaction.ToJSON();
                dataDict.Set(ret, ASAICASHINFW_PROP_TRANSACTION);
                WriteJournal(string.Format("OnEnabledRight  JSON: {0}", ret));
                //It wouuld allowto BASI_IDDLE LOOP to sanitize CR .
                Task t = Task.Run(() =>
                {
                    Stopwatch s = new Stopwatch();
                    s.Start();
                    while (s.Elapsed < TimeSpan.FromSeconds(1))
                    {

                    }
                    s.Stop();
                });
                t.Wait();
                RaiseEvent();
            }
        }

        private void OnDisconnectingLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal($"OnDisconnectingLeft: command {e.commandArgs.command} ResultCode= {e.commandArgs.resultCode} Text: {e.commandArgs.resultText} ");
                    break;
            }
        }

        private void OnDisconnectingRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Right:
                    WriteJournal($"OnDisconnectingRight: command {e.commandArgs.command} ResultCode= {e.commandArgs.resultCode} Text: {e.commandArgs.resultText} ");
                    break;
            }
        }
        private void OnDisabledLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal($"OnDisabledLeft: command {e.commandArgs.command} ResultCode= {e.commandArgs.resultCode} Text: {e.commandArgs.resultText} ");

                    break;
            }
        }

        private void OnDisabledRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Right:
                    WriteJournal($"OnDisabledRight: command {e.commandArgs.command} ResultCode= {e.commandArgs.resultCode} Text: {e.commandArgs.resultText} ");
                    break;
            }
        }

        private void OnConnectingLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal($"OnConnectingLeft: command {e.commandArgs.command} ResultCode= {e.commandArgs.resultCode} Text: {e.commandArgs.resultText} ");
                    switch(e.commandArgs.resultCode)
                    {
                        case BVCommandResultCode.CommandSuccess:
                            break;
                    }
                    break;
            }
        }

        private void OnConnectingRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {                
                case BVPosition.Right:
                    WriteJournal($"OnConnectingRight: command {e.commandArgs.command} ResultCode= {e.commandArgs.resultCode} Text: {e.commandArgs.resultText} ");
                    switch (e.commandArgs.resultCode)
                    {
                        case BVCommandResultCode.CommandSuccess:
                            break;
                    }

                    break;
            }
        }

        private void OnCmdErrorLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal("OnCmdErrorLeft : " + e.deviceArgs.message);
                    leftBVIsEnabled = false;
                    PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR, ASAILOFW_STATUS_ERROR);
                    break;
            }
            WriteJournal($"OnCmdErrorLeft: commandingEnable: leftBVIsEnabled:{leftBVIsEnabled} rightBVIsEnabled:{rightBVIsEnabled}");
            if (!leftBVIsEnabled && !rightBVIsEnabled)
            {
                Transaction transaction = new Transaction
                {
                    type = "ERROR"
                };
                string ret = transaction.ToJSON();
                dataDict.Set(ret, ASAICASHINFW_PROP_TRANSACTION);
                WriteJournal(string.Format("OnCmdErrorLeft  JSON: {0}", ret));
                RaiseEvent();
            }
        }

        private void OnCmdErrorRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
              
                case BVPosition.Right:
                    rightBVIsEnabled = false;
                    WriteJournal("OnCmdErrorRightt: " + e.deviceArgs.message);
                    PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR2, ASAILOFW_STATUS_ERROR);
                    break;

            }
            WriteJournal($"OnCmdErrorRight: commandingEnable: leftBVIsEnabled:{leftBVIsEnabled} rightBVIsEnabled:{rightBVIsEnabled}");
            if (!leftBVIsEnabled && !rightBVIsEnabled)
            {
                Transaction transaction = new Transaction
                {
                    type = "ERROR"
                };
                string ret = transaction.ToJSON();
                dataDict.Set(ret, ASAICASHINFW_PROP_TRANSACTION);
                WriteJournal(string.Format("OnCmdErrorRight  JSON: {0}", ret));
                RaiseEvent();
            }
        }

        private void OnCmdResponse(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal("OnCmdResponse"  + e.deviceArgs.message );
                    break;
                case BVPosition.Right:
                    WriteJournal("OnCmdResponse: "  + e.deviceArgs.message );
                    break;
            }
        }


        private void OnCheatedRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Right:
                    WriteJournal("OnCheatedRight" + Environment.NewLine
                                + "Type:" + e.documentArgs.type + Environment.NewLine
                                + "Currency:" + e.documentArgs.currency + Environment.NewLine
                                + "Denomination:" + e.documentArgs.denomination + Environment.NewLine
                                + "Barcode:" + e.documentArgs.barcode + Environment.NewLine);

                    Transaction transaction = GetTransactionFromDataDict();
                    

                    if (transaction != null)
                    {
                        string ret = transaction.ToJSON();
                        WriteJournal((String.Format("OnCheatedRight JSON {0}", ret)));
                        if (transaction.type == "TR")
                        {
                            string jsonRight = JsonConvert.SerializeObject(righttickets);
                            WriteJournal(String.Format("OnCheatedRight RightTickets {0} ", jsonRight));

                            righttickets.RemoveAt(righttickets.Count - 1);
                            rightticketsStatus.RemoveAt(rightticketsStatus.Count - 1);

                            if (!getOutOfWelcome)
                            {
                                getOutOfWelcome = true;
                                RaiseEvent();

                            }
                        }
                        else
                        {
                            RaiseEvent2(ASAICASHIFW_EVT_BB_FAILED);
                        }

                    }
                    PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR2, ASAILOFW_STATUS_ERROR);


                    break;
                default:
                    WriteJournal("OnCheatedRight Default");
                    break;
            }
        }

        private void OnCheatedLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal("OnCheatedLeft" + Environment.NewLine
                                + "Type:" + e.documentArgs.type + Environment.NewLine
                                + "Currency:" + e.documentArgs.currency + Environment.NewLine
                                + "Denomination:" + e.documentArgs.denomination + Environment.NewLine
                                + "Barcode:" + e.documentArgs.barcode + Environment.NewLine);

                    Transaction transaction = GetTransactionFromDataDict();
                   
                    if (transaction != null)
                    {
                        string ret = transaction.ToJSON();
                        WriteJournal((String.Format("OnCheatedLeft JSON {0}", ret)));
                        if (transaction.type == "TR")
                        {
                            string jsonLeft = JsonConvert.SerializeObject(lefttickets);
                            WriteJournal(String.Format("OnCheatedLeft LeftTickets {0} ", jsonLeft));

                            lefttickets.RemoveAt(lefttickets.Count - 1);
                            leftticketsStatus.RemoveAt(leftticketsStatus.Count - 1);

                            if (!getOutOfWelcome)
                            {
                                getOutOfWelcome = true;
                                RaiseEvent();
                            }

                        }
                        else
                        {
                            RaiseEvent2(ASAICASHIFW_EVT_BB_FAILED);
                        }
                    }
                    PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR, ASAILOFW_STATUS_ERROR);
                    break;
                default:
                    WriteJournal("OnCheatedLeft Default");
                    break;
            }
        }

        private void OnPUPEscrowLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal("OnPUPEscrowLeft"  + e.deviceArgs.message );
                    api.meiCmdLeft.EscrowReturn();
                    break;
               
            }
        }

        private void OnPUPEscrowRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Right:
                    WriteJournal("OnPUPEscrowRight: "  + e.deviceArgs.message );
                    api.meiCmdRight.EscrowReturn();
                    break;
            }
        }

        private void OnStackedLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            try
            {
                switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
                {
                    case BVPosition.Left:
                        if (api.meiRight.autoCreditOnStack)
                        {
                            WriteJournal("OnStackedLeft:Credited" + Environment.NewLine
                                + "Type:" + e.documentArgs.type + Environment.NewLine
                                + "Currency:" + e.documentArgs.currency + Environment.NewLine
                                + "Denomination:" + e.documentArgs.denomination + Environment.NewLine
                                + "Barcode:" + e.documentArgs.barcode + Environment.NewLine);
                        }
                        if (e.documentArgs.type.ToUpper() == "BARCODE")
                        {
                            decimal amount = lefttickets.Find(p => p.Key == e.documentArgs.barcode).Value;
                            int index = leftticketsStatus.FindIndex(p => p.Key == e.documentArgs.barcode);
                            leftticketsStatus[index] = new KeyValuePair<string, bool>(e.documentArgs.barcode, true);

                            WriteJournal(String.Format("OnStackedLeft Redeem {0} {1}", e.documentArgs.barcode, amount) );
                            string jsonLeft = JsonConvert.SerializeObject(lefttickets);
                            WriteJournal(String.Format("OnStackedLeft LeftTickets {0} ", jsonLeft));
                            string jsonRight = JsonConvert.SerializeObject(righttickets);
                            WriteJournal(String.Format("OnStackedLeft RightTickets {0} ", jsonRight));

                            jsonLeft = JsonConvert.SerializeObject(leftticketsStatus);
                            WriteJournal(String.Format("OnStackedLeft LeftTicketsStatus {0} ", jsonLeft));
                            jsonRight = JsonConvert.SerializeObject(rightticketsStatus);
                            WriteJournal(String.Format("OnStackedLeft RightTicketsStatus {0} ", jsonRight));

                            string seqNumber = UpdateJson(lefttickets, leftticketsStatus, righttickets, rightticketsStatus);
                            DisplayTickets(lefttickets, leftticketsStatus, righttickets, rightticketsStatus);

                            bool redeem = Redeem(seqNumber, amount, e.documentArgs.barcode);
                            WriteJournal(String.Format("OnStackedLeft Redeem {0} {1} {2}", e.documentArgs.barcode, amount, redeem));
                            PostTransaction();
                            PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR, ASAILOFW_STATUS_OK);
                            PostCounters();
                            if (!getOutOfWelcome)
                            {
                                getOutOfWelcome = true;
                                RaiseEvent();
                            }
                            else { 
                                if (!getOutOfMultipleTickets)
                                {
                                    getOutOfMultipleTickets = true;
                                    RaiseEvent();
                                }
                            }
                        }
                        else
                        {
                            if (e.documentArgs.type.ToUpper() == "BILL")
                            {
                                checkStackingEventTmr.Stop();
                                WriteJournal(String.Format("OnStackedLeft BIll {0} {1}", e.documentArgs.type, e.documentArgs.denomination) );
                                PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR, ASAILOFW_STATUS_OK);
                                PostCounters();
                                FrmSendEvent(ASAICASHIFW_EVT_STACKED);
                            }
                        }
                        break;
                    default:
                        WriteJournal("OnStackedRight Default" );
                        break;
                }
            }
            catch (Exception ex)
            {
                WriteJournal(String.Format("OnStackedRight Exception {0} {1} ", ex.Message, ex.StackTrace) );
            }
        }

        private void OnStackedRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            try
            {
                switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
                {
                    case BVPosition.Right:
                        if (api.meiRight.autoCreditOnStack)
                        {
                            WriteJournal("OnStackedRight:Credited" + Environment.NewLine
                                + "Type:" + e.documentArgs.type + Environment.NewLine
                                + "Currency:" + e.documentArgs.currency + Environment.NewLine
                                + "Denomination:" + e.documentArgs.denomination + Environment.NewLine
                                + "Barcode:" + e.documentArgs.barcode + Environment.NewLine);


                        }
                        if (e.documentArgs.type.ToUpper() == "BARCODE")
                        {
                            decimal amount = righttickets.Find(p => p.Key == e.documentArgs.barcode).Value;
                            int index = rightticketsStatus.FindIndex(p => p.Key == e.documentArgs.barcode);
                            rightticketsStatus[index] = new KeyValuePair<string, bool>(e.documentArgs.barcode, true);

                            WriteJournal(String.Format("OnStackedRight Redeem {0} {1}", e.documentArgs.barcode, amount));
                            string jsonLeft = JsonConvert.SerializeObject(lefttickets);
                            WriteJournal(String.Format("OnStackedRight LeftTickets {0} ", jsonLeft));
                            string jsonRight = JsonConvert.SerializeObject(righttickets);
                            WriteJournal(String.Format("OnStackedRight RightTickets {0} ", jsonRight));

                            jsonLeft = JsonConvert.SerializeObject(leftticketsStatus);
                            WriteJournal(String.Format("OnStackedRight LeftTicketsStatus {0} ", jsonLeft));
                            jsonRight = JsonConvert.SerializeObject(rightticketsStatus);
                            WriteJournal(String.Format("OnStackedRight RightTicketsStatus {0} ", jsonRight));

                            string seqNumber = UpdateJson(lefttickets, leftticketsStatus, righttickets, rightticketsStatus);
                            DisplayTickets(lefttickets, leftticketsStatus, righttickets, rightticketsStatus);


                            bool redeem = Redeem(seqNumber, amount, e.documentArgs.barcode);
                            WriteJournal(String.Format("OnStackedRight Redeem {0} {1} {2}", e.documentArgs.barcode, amount, redeem) );
                            PostTransaction();
                            PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR, ASAILOFW_STATUS_OK);
                            PostCounters();
                            if (!getOutOfWelcome)
                            {
                                getOutOfWelcome = true;
                                RaiseEvent();
                            }
                            else
                            {
                                if (!getOutOfMultipleTickets)
                                {
                                    getOutOfMultipleTickets = true;
                                    RaiseEvent();
                                }
                            }

                        }
                        else
                        {
                            if (e.documentArgs.type.ToUpper() == "BILL")
                            {
                                checkStackingEventTmr.Stop();
                                WriteJournal(String.Format("OnStackedRigh BIll {0} {1}", e.documentArgs.type, e.documentArgs.denomination) );
                                //api.EnableAcceptance(api.meiRight, false);
                                PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR, ASAILOFW_STATUS_OK);
                                PostCounters();
                                FrmSendEvent(ASAICASHIFW_EVT_STACKED);
                            }
                        }
                        break;
                    default:
                        WriteJournal("OnStackedRight Default" );
                        break;
                }
            }
            catch(Exception ex)
            {
                WriteJournal(String.Format("OnStackedRight Exception {0} {1} ", ex.Message, ex.StackTrace) );
            }
        }

        private void OnReturnedLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal("OnReturnedLeft:" + e.documentArgs.barcode);
                    int index = leftticketsStatus.FindIndex(p => p.Key == e.documentArgs.barcode);
                    if (index != -1)
                    {
                        lefttickets.RemoveAt(index);
                        leftticketsStatus.RemoveAt(index);
                    }
                    break;
                default:
                    WriteJournal("OnReturnedLeft Default" );
                    break;
            }
        }

        private void OnReturnedRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Right:
                    WriteJournal("OnReturnedRight:" + e.documentArgs.barcode);
                    int index = rightticketsStatus.FindIndex(p => p.Key == e.documentArgs.barcode);
                    if (index != -1)
                    {
                        righttickets.RemoveAt(index);
                        rightticketsStatus.RemoveAt(index);
                    }
                    break;
          
                default:
                    WriteJournal("OnReturnedRight Default" );
                    break;
            }
        }

        private void OnCashBoxRemovedLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal("OnCashBoxRemovedLeft" );
                    break;
                default:
                    WriteJournal("OnCashBoxRemovedLeft Default" );
                    break;
            }
        }

        private void OnCashBoxRemovedRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Right:
                    WriteJournal("OnCashBoxRemovedRight" );
                    break;
                default:
                    WriteJournal("OnCashBoxRemovedRight Default" );
                    break;
            }
        }


        private void OnCashBoxAttachedLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal("OnCashBoxAttachedLeft" );
                    break;
                default:
                    WriteJournal("OnCashBoxAttachedLeft Default" );
                    break;
            }
        }

        private void OnCashBoxAttachedRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Right:
                    WriteJournal("OnCashBoxAttachedRight" );
                    break;
                default:
                    WriteJournal("OnCashBoxAttachedRight Default" );
                    break;
            }
        }

        private void OnStackerFullLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal("OnStackerFullLeft");
                    Transaction transaction = GetTransactionFromDataDict();
                    if (transaction != null)
                    {
                        string ret = transaction.ToJSON();
                        WriteJournal((String.Format("OnStackerFullLeft JSON {0}", ret)));
                        if (transaction.type == "TR")
                        {
                            if (!getOutOfWelcome)
                            {
                                getOutOfWelcome = true;
                                RaiseEvent();
                            }
                        }
                        else
                            RaiseEvent();

                    }
                    PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR, ASAILOFW_STATUS_ERROR);
                    break;
                default:
                    WriteJournal("OnStackerFullLeft Default");
                    break;
            }
        }

        private void OnStackerFullRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Right:
                    WriteJournal("OnStackerFullRight");
                    Transaction transaction = GetTransactionFromDataDict();
                    if (transaction != null)
                    {
                        string ret = transaction.ToJSON();
                        WriteJournal((String.Format("OnStackerFullRight JSON {0}", ret)));
                        if (transaction.type == "TR")
                        {
                            if (!getOutOfWelcome)
                            {
                                getOutOfWelcome = true;
                                RaiseEvent();
                            }
                        }
                        else
                            RaiseEvent();
                    }
                    PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR2, ASAILOFW_STATUS_ERROR);


                    break;
                default:
                    WriteJournal("OnStackerFullRight Default");
                    break;
            }
        }


        private void OnStackerFullClearedLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal("OnStackerFullClearedLeft" );
                    break;
                default:
                    WriteJournal("OnStackerFullClearedLeft Default" );
                    break;
            }
        }

        private void OnStackerFullClearedRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Right:
                    WriteJournal("OnStackerFullClearedRight" );
                    break;
                default:
                    WriteJournal("OnStackerFullClearedRight Default" );
                    break;
            }
        }
        private void OnRejectedLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal("OnRejectedLeft" );
                    break;
               
                default:
                    WriteJournal("OnRejectedLeft Default" );
                    break;
            }
        }

        private void OnRejectedRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Right:
                    WriteJournal("OnRejectedRight" );
                    break;
                default:
                    WriteJournal("OnRejectedRight Default" );
                    break;
            }
        }

        private void OnEscrowLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            //ReadMinimizeShortPay();
            string ret = string.Empty;
            Transaction transaction = null;
            try
            {
                switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
                {
                    case BVPosition.Left:
                        WriteJournal("OnEscrowLeft" + Environment.NewLine
                               + "Type:" + e.documentArgs.type + Environment.NewLine
                               + "Currency:" + e.documentArgs.currency + Environment.NewLine
                               + "Denomination:" + e.documentArgs.denomination + Environment.NewLine
                               + "Barcode:" + e.documentArgs.barcode + Environment.NewLine);
                        if (e.documentArgs.type.ToUpper() == "BARCODE")
                        {
                            if (BillOnEscrow)
                            {
                                //There is bill in other bV
                                api.meiCmdLeft.EscrowReturn();
                                WriteJournal("ASAICashInFW - OnEscrowLeft: Returning Ticket located on Left " + ret);
                                return;
                            }
                            bool startUp = StartUp();
                            if (startUp)
                            {
                                string seqNumber = DateTime.Now.ToString("MMddyyyyHHmmssff");
                                decimal amount = Validate(seqNumber, e.documentArgs.barcode);
                                if (amount > 0)
                                {
                                    decimal total = GetTicketsSessionTotal(amount);
                                    if ((total < MAX_TICKETSTRANSACTION_VALUE) && (lefttickets.Count + righttickets.Count < MAX_TICKETSTRANSACTION_COUNT))
                                    {
                                        bool canBills = true;
                                        bool canCoins = true;

                                        WriteJournal("ASAICashInFW - OnEscrowLeft minimizeShortPay: " + minimizeShortPay);

                                        if (minimizeShortPay)
                                        {
                                            canBills = this.CanDispenseBills(total);
                                            canCoins = this.CanDispenseCoins(total);
                                        }

                                        WriteJournal($"ASAICashInFW - OnEscrowLeftStack Counters Validation: Bills {canBills} Coins {canCoins}");
                                        if (canBills && canCoins)
                                        {
                                            if (BillOnEscrow)
                                            {
                                                //There is bill in other bV
                                                api.meiCmdLeft.EscrowReturn();
                                                Cancel(seqNumber, amount, e.documentArgs.barcode);
                                                WriteJournal("ASAICashInFW - OnEscrowLeft: Returning Ticket located on Left beacuse there is a Bill running ");
                                                return;
                                            }

                                            if (lefttickets.Count == 0 && righttickets.Count == 0)
                                            {
                                                transaction = new Transaction
                                                {
                                                    type = "TR",
                                                    seqNumber = seqNumber,
                                                    total = (decimal)0,
                                                    items = new List<Item>()
                                                };
                                                ret = transaction.ToJSON();
                                                dataDict.Set(ret, ASAICASHINFW_PROP_TRANSACTION);
                                            }
                                            else
                                            {
                                                dataDict.Get(ref ret, ASAICASHINFW_PROP_TRANSACTION);
                                                transaction = DeserializeJSON(ret);
                                            }
                                            WriteJournal("ASAICashInFW - OnEscrowLeft transaction: " + ret);
                                            lefttickets.Add(new KeyValuePair<string, decimal>(e.documentArgs.barcode, amount));
                                            leftticketsStatus.Add(new KeyValuePair<string, bool>(e.documentArgs.barcode, false));
                                            //Comment for Dispense Button Problem
                                            //if (!getOutOfWelcome)
                                            //{
                                            //    getOutOfWelcome = true;
                                            //    RaiseEvent();
                                            //}
                                            WriteJournal("ASAICashInFW - LeftStack");

                                                //_leftCounters_t_1 = api.meiCmdLeft.GetCounters();
                                                //_rightCounters_t_1 = api.meiCmdRight.GetCounters();
                                            api.meiCmdLeft.EscrowStack();
                                        }
                                        else
                                        {      
                                            WriteJournal($"OnEscrowLEft received a new ticket  {e.documentArgs.barcode} but it is not dispensable and will be canceled and spit out");
                                            api.meiCmdLeft.EscrowReturn();
                                            Cancel(seqNumber, amount, e.documentArgs.barcode);
                                        }
                                    }
                                    else
                                    {
                                        if (!getOutOfWelcome)
                                        {
                                            if (lefttickets.Count + righttickets.Count >0) {
                                                getOutOfWelcome = true;
                                                RaiseEvent();
                                            }
                                        }
                                        WriteJournal($"OnEscrowLEft received a new ticket  {e.documentArgs.barcode} but exceeded the MAX VALUE adn will be canceled and spit out");
                                        api.meiCmdLeft.EscrowReturn();
                                        Cancel(seqNumber, amount, e.documentArgs.barcode);
                                    }                                   
                                }
                               else
                               {
                                   WriteJournal("ASAICashInFW - EscrowReturnLeft. Invalid Validate");
                                   api.meiCmdLeft.EscrowReturn();
                               }
                           }
                           else
                           {
                               WriteJournal("ASAICashInFW - EscrowReturnLeft. Startup TR Factory is not OK");
                               api.meiCmdLeft.EscrowReturn();
                           }
                       }
                       else
                       {
                            if (e.documentArgs.type.ToUpper() == "BILL")
                            {
                                if (lefttickets.Count > 0 || righttickets.Count > 0)
                                {

                                    api.meiCmdLeft.EscrowReturn();
                                    WriteJournal(String.Format("OnEscrowLeft: A bill {0} was inserted in the middle of TR and it was spit out", e.documentArgs.denomination));
                                }
                                else
                                {
                                    if (!BillOnEscrow)
                                    {
                                        BillOnEscrow = true;
                                        transaction = new Transaction
                                        {
                                            type = "BB",
                                            seqNumber = DateTime.Now.ToString("MMddyyyyHHmmssff"),
                                            total = decimal.Parse(e.documentArgs.denomination),
                                            items = new List<Item>()
                                        };
                                        Item item = new Item
                                        {
                                            index = 1,
                                            code = "BB",
                                            value = decimal.Parse(e.documentArgs.denomination),
                                            billAcceptor = (int)BVConf.Left
                                        };
                                        transaction.items.Add(item);
                                        ret = transaction.ToJSON();
                                        WriteJournal(String.Format("OnEscrowLeft: A bill {0} was inserted  {1}", e.documentArgs.denomination, ret));
                                        dataDict.Set(ret, ASAICASHINFW_PROP_TRANSACTION);
                                        //Spit out anything in the another BV.
                                        api.meiCmdRight.EscrowReturn();
                                        api.meiCmdRight.EnableAcceptance(false);
                                        WriteJournal("OnEscrowLeft: Disabling BV on right");
                                    }
                                    else {
                                        api.meiCmdLeft.EscrowReturn();
                                        WriteJournal($"OnEscrowLeft: A bill { e.documentArgs.denomination} was inserted in an existing BB and  it was spit out");
                                    }

                                }
                                if (!getOutOfWelcome)
                                {
                                    getOutOfWelcome = true;
                                    RaiseEvent();
                                }
                                   
                                }
                            }

                        break;
                   default:
                       WriteJournal("OnEscrowLeft Default" );
                       break;
               }
           }
           catch(Exception ex)
           {
               api.meiCmdLeft.EscrowReturn();
               WriteJournal($"OnEscrowLeft Exception { ex.Message} {ex.StackTrace}");
           }
       }

       private void OnEscrowRight(object sender, MEI_BV_Devices.MeiEventArgs e)
       {
            //ReadMinimizeShortPay();
            string ret = string.Empty;
           Transaction transaction = null;

            try
            {
               switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
               {
                   case BVPosition.Right:
                       WriteJournal("OnEscrowRight" + Environment.NewLine
                              + "Type:" + e.documentArgs.type + Environment.NewLine
                              + "Currency:" + e.documentArgs.currency + Environment.NewLine
                              + "Denomination:" + e.documentArgs.denomination + Environment.NewLine
                              + "Barcode:" + e.documentArgs.barcode + Environment.NewLine);
                       if (e.documentArgs.type.ToUpper() == "BARCODE")
                       {
                            if (BillOnEscrow)
                            {
                                //There is bill in other bV
                                api.meiCmdRight.EscrowReturn();
                                WriteJournal("ASAICashInFW - OnEscrowLeft: Returning Ticket located on right " + ret);
                                return;
                            }
                           bool startUp = StartUp();
                           if (startUp)
                           {
                               string seqNumber = DateTime.Now.ToString("MMddyyyyHHmmssff");
                               decimal amount = Validate(seqNumber, e.documentArgs.barcode);
                               if (amount > 0)
                               {
                                    decimal total = GetTicketsSessionTotal(amount);
                                    if ((total < MAX_TICKETSTRANSACTION_VALUE) && (lefttickets.Count + righttickets.Count < MAX_TICKETSTRANSACTION_COUNT))
                                    {
                                        bool canBills = true;
                                        bool canCoins = true;

                                        WriteJournal("ASAICashInFW - OnEscrowRight minimizeShortPay: " + minimizeShortPay);

                                        if (minimizeShortPay)
                                        {
                                            canBills = this.CanDispenseBills(total);
                                            canCoins = this.CanDispenseCoins(total);
                                        }

                                        WriteJournal($"ASAICashInFW - OnEscrowRightStack Counters Validation: Bills {canBills} Coins {canCoins}");
                                        if (canBills && canCoins)
                                        {
                                            if (BillOnEscrow)
                                            {
                                                //There is bill in other bV
                                                api.meiCmdRight.EscrowReturn();
                                                Cancel(seqNumber, amount, e.documentArgs.barcode);
                                                WriteJournal("ASAICashInFW - OnEscrowRight: Returning Ticket located on Right beacuse there is a Bill running ");
                                                return;
                                            }
                                            if (lefttickets.Count == 0 && righttickets.Count == 0)
                                            {
                                                transaction = new Transaction
                                                {
                                                    type = "TR",
                                                    seqNumber =seqNumber,
                                                    total = (decimal)0,
                                                    items = new List<Item>()
                                                };
                                                ret = transaction.ToJSON();
                                                dataDict.Set(ret, ASAICASHINFW_PROP_TRANSACTION);


                                            }
                                            else
                                            {
                                                dataDict.Get(ref ret, ASAICASHINFW_PROP_TRANSACTION);
                                                transaction = DeserializeJSON(ret);
                                            }
                                            WriteJournal("ASAICashInFW - OnEscrowRight transaction: " + ret);
                                            righttickets.Add(new KeyValuePair<string, decimal>(e.documentArgs.barcode, amount));
                                            rightticketsStatus.Add(new KeyValuePair<string, bool>(e.documentArgs.barcode, false));
                                            //same dispense button problem
                                            //if (!getOutOfWelcome)
                                            //{
                                            //    getOutOfWelcome = true;
                                            //    RaiseEvent();
                                            //}
                                            WriteJournal("ASAICashInFW - RightStack");
                                            //_leftCounters_t_1 = api.meiCmdLeft.GetCounters();
                                            //_rightCounters_t_1 = api.meiCmdRight.GetCounters();
                                            api.meiCmdRight.EscrowStack();
                                        }
                                        else
                                        {
                                            WriteJournal($"OnEscrowRight received a new ticket  {e.documentArgs.barcode} but it is not disensable and will be canceled and spit out");
                                            api.meiCmdRight.EscrowReturn();
                                            Cancel(seqNumber, amount, e.documentArgs.barcode);
                                        }
                                    }
                                    else
                                    {
                                        if (!getOutOfWelcome)
                                        {
                                            if (lefttickets.Count + righttickets.Count > 0)
                                            {
                                                getOutOfWelcome = true;
                                                RaiseEvent();
                                            }
                                        }
                                        WriteJournal($"OnEscrowRight received a new ticket  {e.documentArgs.barcode} but exceeded the MAX VALUE and will be canceled and spit out");
                                        api.meiCmdRight.EscrowReturn();
                                        Cancel(seqNumber, amount, e.documentArgs.barcode);
                                    }                                  
                               }
                               else
                               {
                                   WriteJournal("ASAICashInFW - OnEscrowRightReturn. Invalid Validate");
                                   api.meiCmdRight.EscrowReturn();
                               }
                           }
                           else
                           {
                               WriteJournal("ASAICashInFW - OnEscrowRightReturn. Startup TR Factory is not OK");
                               api.meiCmdRight.EscrowReturn();
                           }
                       }
                       else
                       {
                            //BB
                            if (e.documentArgs.type.ToUpper() == "BILL")
                            {
                               
                                if (lefttickets.Count > 0 || righttickets.Count > 0)
                                {
                                    api.meiCmdRight.EscrowReturn();
                                    WriteJournal(String.Format("OnEscrowRight: A bill {0} was inserted in the middle of TR and it was spit out", e.documentArgs.denomination));
                                }
                                else
                                {
                                    if (!BillOnEscrow)
                                    {
                                        BillOnEscrow = true;
                                        transaction = new Transaction
                                        {
                                            type = "BB",
                                            seqNumber = DateTime.Now.ToString("MMddyyyyHHmmssff"),
                                            total = decimal.Parse(e.documentArgs.denomination),
                                            items = new List<Item>()
                                        };
                                        Item item = new Item
                                        {
                                            index = 1,
                                            code = "BB",
                                            value = decimal.Parse(e.documentArgs.denomination),
                                            billAcceptor = (int)BVConf.Right
                                        };
                                        transaction.items.Add(item);
                                        ret = transaction.ToJSON();
                                        dataDict.Set(ret, ASAICASHINFW_PROP_TRANSACTION);
                                        //Spit out anything in the another BV.
                                        api.meiCmdLeft.EscrowReturn();
                                        api.meiCmdLeft.EnableAcceptance(false);
                                        WriteJournal("OnEscrowRight: Disabling BV on left");
                                    }
                                    else
                                    {
                                        api.meiCmdRight.EscrowReturn();
                                        WriteJournal($"OnEscrowRight: A bill {e.documentArgs.denomination} was inserted in an existing BB and it was spit out");
                                    }


                                    if (!getOutOfWelcome)
                                    {
                                        getOutOfWelcome = true;
                                        RaiseEvent();
                                    }
                                 }
                            }
                        }
                        break;
                   default:
                       WriteJournal("OnEscrowRight Default" );
                       break;
               }
           }
           catch (Exception ex)
           {
               api.meiCmdRight.EscrowReturn();
               WriteJournal($"OnEscrowRight Exception {ex.Message} {ex.StackTrace}");
           }
       }

        private void OnJamDetectedLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal("OnJamDetectedLeft");
                    Transaction transaction = GetTransactionFromDataDict();
                    if (transaction != null)
                    {
                        string ret = transaction.ToJSON();
                        WriteJournal((String.Format("OnJamDetectedLeft JSON {0}", ret)));
                        if (transaction.type == "TR")
                        {
                            if (!getOutOfWelcome)
                            {
                                getOutOfWelcome = true;
                                RaiseEvent();
                            }
                        }
                        else
                           RaiseEvent();
                        
                    }
                    PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR, ASAILOFW_STATUS_ERROR);
                    break;

                default:
                    WriteJournal("OnJamDetectedLeft Default");
                    break;
            }
        }
        private void OnJamDetectedRight(object sender, MEI_BV_Devices.MeiEventArgs e)
        {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Right:
                    WriteJournal("OnJamDetectedRight");
                    Transaction transaction = GetTransactionFromDataDict();
                    if (transaction != null)
                    {
                        string ret = transaction.ToJSON();
                        WriteJournal((String.Format("OnJamDetectedRight JSON {0}", ret)));
                        if (transaction.type == "TR")
                        {
                            if (!getOutOfWelcome)
                            {
                                getOutOfWelcome = true;
                                RaiseEvent();
                            }
                        }
                        else
                            RaiseEvent();

                    }
                    PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR2, ASAILOFW_STATUS_ERROR);
                    break;
                default:
                    WriteJournal("OnJamDetectedRight Default");
                    break;
            }
        }
        private void OnJamClearedLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
       {
           switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
           {
               case BVPosition.Left:
                   WriteJournal("OnJamClearedLeft" );
                   break;
               default:
                   WriteJournal("OnJamClearedLeft Default" );
                   break;
           }
       }

       private void OnJamClearedRight(object sender, MEI_BV_Devices.MeiEventArgs e)
       {
           switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
           {
               case BVPosition.Right:
                   WriteJournal("OnJamClearedRight" );
                   break;
               default:
                   WriteJournal("OnJamClearedRight Default" );
                   break;
           }
       }
       private void OnNoteRetrievedLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
       {
           switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
           {
               case BVPosition.Left:
                   WriteJournal("OnNoteRetrievedLeft" );
                   break;

               default:
                   WriteJournal("OnNoteRetrievedLeft Default" );
                   break;
           }
       }

       private void OnNoteRetrievedRight(object sender, MEI_BV_Devices.MeiEventArgs e)
       {
           switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
           {
               case BVPosition.Right:
                   WriteJournal("OnNoteRetrievedRight" );
                   break;
               default:
                   WriteJournal("OnNoteRetrievedRight Default" );
                   break;
           }
       }
       private void OnFailureDetectedLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
       {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal("OnFailureDetectedLeft");
                    Transaction transaction = GetTransactionFromDataDict();
                    if (transaction != null)
                    {
                        string ret = transaction.ToJSON();
                        WriteJournal((String.Format("OnFailureDetectedLeft JSON {0}", ret)));
                        if (transaction.type == "TR")
                        {
                            if (!getOutOfWelcome)
                            {
                                getOutOfWelcome = true;
                                RaiseEvent();
                            }
                        }
                        else
                            RaiseEvent();

                    }
                    PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR, ASAILOFW_STATUS_ERROR);
                    break;

                default:
                    WriteJournal("OnFailureDetectedLeft Default");
                    break;
            }

        }

        private void OnFailureDetectedRight(object sender, MEI_BV_Devices.MeiEventArgs e)
       {
            switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
            {
                case BVPosition.Left:
                    WriteJournal("OnFailureDetectedRight");
                    Transaction transaction = GetTransactionFromDataDict();
                    if (transaction != null)
                    {
                        string ret = transaction.ToJSON();
                        WriteJournal((String.Format("OnFailureDetectedRight JSON {0}", ret)));
                        if (transaction.type == "TR")
                        {
                            if (!getOutOfWelcome)
                            {
                                getOutOfWelcome = true;
                                RaiseEvent();
                            }
                        }
                        else
                            RaiseEvent();

                    }
                    PostStatusBillAcceptor(ContractStatusDevice.BILLACCEPTOR, ASAILOFW_STATUS_ERROR);
                    break;

                default:
                    WriteJournal("OnFailureDetectedRight Default");
                    break;
            }
        }

       private void OnFailureClearedLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
       {
           switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
           {
               case BVPosition.Left:
                   WriteJournal("OnFailureClearedLeft" );
                   break;
               default:
                   WriteJournal("OnFailureClearedLeft Default" );
                   break;
           }
       }

       private void OnFailureClearedRight(object sender, MEI_BV_Devices.MeiEventArgs e)
       {
           switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
           {

               case BVPosition.Right:
                   WriteJournal("OnFailureClearedRight" );
                   break;
               default:
                   WriteJournal("OnFailureClearedRight Default" );
                   break;
           }
       }



       private void OnConnectedLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
       {
           switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
           {
               case BVPosition.Left:
                    WriteJournal("OnConnectedLeft" );
                    break;
               default:
                   WriteJournal("OnConnectedLeft Default" );
                   break;
           }
       }

       private void OnConnectedRight(object sender, MEI_BV_Devices.MeiEventArgs e)
       {
           switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
           {
               case BVPosition.Right:
                    WriteJournal("OnConnectedRight" );
                    break;
               default:
                   WriteJournal("OnConnectedRight Default" );
                   break;
           }
       }


       private void OnDisconnectedLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
       {
           switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
           {
               case BVPosition.Left:
                   WriteJournal("OnDisconnectedLeft" );
                   break;
               default:
                   WriteJournal("OnDisconnectedLeft Default" );
                   break;
           }
       }

       private void OnDisconnectedRight(object sender, MEI_BV_Devices.MeiEventArgs e)
       {
           switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
           {
               case BVPosition.Right:
                   WriteJournal("OnDisconnectedRight" );
                   break;
               default:
                   WriteJournal("OnDisconnectedRight Default" );
                   break;
           }
       }
       private void OnPowerUpLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
       {

           switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
           {
               case BVPosition.Left:
                    try
                    {
                        WriteJournal("OnPowerUpLeft");                                              
                    }
                    catch (Exception ex)
                    {
                        WriteJournal("OnPowerUpLeft error: " + ex.Message);
                    }
                   break;
               default:
                   WriteJournal("OnPowerUpLeft Default" );
                   break;
           }
       }

       private void OnPowerUpRight(object sender, MEI_BV_Devices.MeiEventArgs e)
       {

           switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
           {
               case BVPosition.Right:       
                    try
                    {
                        WriteJournal("OnPowerUpRight");                      
                    }
                    catch (Exception ex)
                    {
                        WriteJournal("OnPowerUpRight error: " + ex.Message);
                    }
                    break;
               default:
                   WriteJournal("OnPowerUpRight Default" );
                   break;
           }
       }

       private void OnPowerUpCompleteLeft(object sender, MEI_BV_Devices.MeiEventArgs e)
       {
           switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
           {
               case BVPosition.Left:
                    try
                    {
                        WriteJournal("OnPowerUpCompleteLeft");                       
                        string result = "The device was reset.";                     

                        RegistryKey myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_RESULT", true);
                        if (myKey != null)
                        {
                            myKey.SetValue("STRING", result, RegistryValueKind.String);
                            myKey.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteJournal("OnPowerUpCompleteLeft error: " + ex.Message);
                    }

                    break;

               default:
                   WriteJournal("OnPowerUpCompleteLeft Default" );
                   break;
           }
       }

       private void OnPowerUpCompleteRight(object sender, MEI_BV_Devices.MeiEventArgs e)
       {
           switch ((BVPosition)Enum.Parse(typeof(BVPosition), e.deviceArgs.position))
           {

               case BVPosition.Right:
                    try
                    {

                        WriteJournal("OnPowerUpCompleteRight");                  
                        string result = "The device was reset.";
                       
                        RegistryKey myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_RESULT", true);
                        if (myKey != null)
                        {
                            myKey.SetValue("STRING", result, RegistryValueKind.String);
                            myKey.Close();
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteJournal("OnPowerUpCompleteRight error: " + ex.Message);
                    }
                   
                    break;
               default:
                   WriteJournal("OnPowerUpCompleteRight Default" );
                   break;
           }
       }


       public string UpdateJson(List<KeyValuePair<string, decimal>> lefttickets, List<KeyValuePair<string, bool>> leftticketsstatus,
                                List<KeyValuePair<string, decimal>> righttickets, List<KeyValuePair<string, bool>> rightticketsstatus)

        {
            string seqNumber = string.Empty;

           try
           {
                string ret = string.Empty;
                dataDict.Get(ref ret, ASAICASHINFW_PROP_TRANSACTION);
                WriteJournal($"ASAICashInFW - UpdateJson Property {ret}" );
                if (ret != String.Empty)
                { 
                    Transaction transaction = DeserializeJSON(ret);
                    if (transaction != null)
                    { 
                        seqNumber = transaction.seqNumber;
                        transaction.items.Clear();
                        transaction.total = 0;
                        lefttickets.ForEach(item => {
                            if (leftticketsstatus.Find(x=>x.Key == item.Key).Value)
                            { 
                                Item ticket = new Item
                                {
                                    index = transaction.items.Count + 1,
                                    code = item.Key.Trim(),
                                    value = item.Value,
                                    billAcceptor = (int)BVConf.Left,
                                };
                                transaction.items.Add(ticket);
                                transaction.total = transaction.total + item.Value;
                            }
                        });

                        righttickets.ForEach(item => {
                            if (rightticketsstatus.Find(x => x.Key == item.Key).Value)
                            {
                                Item ticket = new Item
                                {
                                    index = transaction.items.Count + 1,
                                    code = item.Key.Trim(),
                                    value = item.Value,
                                    billAcceptor = (int)BVConf.Right,
                                };
                                transaction.items.Add(ticket);
                                transaction.total = transaction.total + item.Value;
                            }
                        });

                        ret = transaction.ToJSON();
                        dataDict.Set(ret, ASAICASHINFW_PROP_TRANSACTION);
                        WriteJournal("ASAICashInFW - Json:: " + ret);
                    }
                    else
                        WriteJournal("ASAICashInFW - Json:: NULL");
                }
                else
                    WriteJournal("ASAICashInFW - Json:: Dictionary is empty");

            }
            catch (Exception ex)
           {
               WriteJournal("ASAICashInFW - UpdateJson exception: " + ex.Message);
           }

           return seqNumber;
       }


        string UpdateJson(string ticketNumber, decimal amount, BVConf bvSide)
        {
            string seqNumber = string.Empty;

            try
            {
                string ret = string.Empty;
                dataDict.Get(ref ret, ASAICASHINFW_PROP_TRANSACTION);
                WriteJournal($"ASAICashInFW - UpdateJson Property {ret}");
                if (ret != String.Empty)
                {
                    Transaction transaction = DeserializeJSON(ret);
                    if (transaction != null)
                    {
                        seqNumber = transaction.seqNumber;
                        Item item = new Item
                        {
                            index = transaction.items.Count + 1,
                            code = ticketNumber.Trim(),
                            value = amount,
                            billAcceptor = (int)bvSide,
                        };
                        transaction.items.Add(item);
                        transaction.total = transaction.total + amount;
                        ret = transaction.ToJSON();

                        dataDict.Set(ret, ASAICASHINFW_PROP_TRANSACTION);
                        dataDict.Set(transaction.ToHTML(), ASAICASHINFW_PROP_TRANSACTION_HTML);
                        WriteJournal("ASAICashInFW - Json:: " + ret);
                    }
                    else
                        WriteJournal("ASAICashInFW - Json:: NULL");
                }
                else
                    WriteJournal("ASAICashInFW - Json:: Dictionary is empty");

            }
            catch (Exception ex)
            {
                WriteJournal("ASAICashInFW - UpdateJson exception: " + ex.Message);
            }

            return seqNumber;
        }

        public decimal GetTicketsSessionTotal(decimal lastTicketOnEscrow)
        {
           decimal amount =0;
           lefttickets.ForEach(item => {
               amount += item.Value;
            });
            righttickets.ForEach(item => {
                amount += item.Value;
            });
            WriteJournal($"GetTicketsSession  on Session: {amount}");
            amount += lastTicketOnEscrow;
            WriteJournal($"GetTicketsSession included the last ticket on escrow  {amount}");
            return amount;
        }


        public string DisplayTickets(List<KeyValuePair<string, decimal>> lefttickets, List<KeyValuePair<string, bool>> leftticketsstatus,
                                     List<KeyValuePair<string, decimal>> righttickets, List<KeyValuePair<string, bool>> rightticketsstatus)
        {
            decimal total = 0;
            int numberOfTickets = 0;
            StringBuilder html = new StringBuilder("<table id='multipleTickets' class='tbMultTicket'><tr><th>Ticket Number</th><th>Value</th></tr>");
            //html.Append("<caption>Ticket List </caption>");

            lefttickets.ForEach(item => {

                if (leftticketsstatus.Find(x=> x.Key == item.Key).Value)
                { 
                    html.Append("<tr>");
                    html.Append("<td>");
                    html.Append(item.Key);
                    html.Append("</td>");
                    html.Append("<td>");
                    html.Append(Convert.ToDouble(item.Value).ToString("C"));
                    html.Append("</td>");
                    html.Append("</tr>");
                    total = total + item.Value;
                    numberOfTickets++;
                }
            });

            righttickets.ForEach(item => {
                if (rightticketsstatus.Find(x => x.Key == item.Key).Value)
                {
                    html.Append("<tr>");
                    html.Append("<td>");
                    html.Append(item.Key);
                    html.Append("</td>");
                    html.Append("<td>");
                    html.Append(Convert.ToDouble(item.Value).ToString("C"));
                    html.Append("</td>");
                    html.Append("</tr>");
                    total = total + item.Value;
                    numberOfTickets++;

                }
            });
            html.Append("<tr>");
            html.Append("<td>");
            html.Append($"Total Tickets: [{numberOfTickets}]");
            html.Append("</td>");
            html.Append("<td>");
            html.Append(Convert.ToDouble(total).ToString("C"));
            html.Append("</td>");
            html.Append("</tr>");

            html.Append("</table>");
            dataDict.Set(html.ToString(), ASAICASHINFW_PROP_TRANSACTION_HTML);
            return html.ToString();

        }
        //THis function would be used for when we found collisions. It comes soon and we might have a potentita issues with collisions on Tickets
        void ConsolidateTicketsTransaction(List<KeyValuePair<string, decimal>> lefttickets, List<KeyValuePair<string, decimal>> rightTickets)
        {
            try
            {
                string ret = string.Empty;
                dataDict.Get(ref ret, ASAICASHINFW_PROP_TRANSACTION);
                Transaction transaction = DeserializeJSON(ret);
                foreach ( var ticket in lefttickets)
                {
                    Item item = new Item
                    {
                        index = transaction.items.Count + 1,
                        code = ticket.Key.Trim(),
                        value = decimal.Parse(ticket.Value.ToString()),
                        billAcceptor = (int)BVConf.Left
                    };
                    transaction.items.Add(item);
                    transaction.total = transaction.total + item.value;
                }
                foreach (var ticket in rightTickets)
                {
                    Item item = new Item
                    {
                        index = transaction.items.Count + 1,
                        code = ticket.Key.Trim(),
                        value = decimal.Parse(ticket.Value.ToString()),
                        billAcceptor = (int)BVConf.Right
                    };
                    transaction.items.Add(item);
                    transaction.total = transaction.total + item.value;
                }

                ret = transaction.ToJSON();
                dataDict.Set(ret, ASAICASHINFW_PROP_TRANSACTION);
                dataDict.Set(transaction.ToHTML(), ASAICASHINFW_PROP_TRANSACTION_HTML);
                WriteJournal("ASAICashInFW - ConsolidateTicketTransaction - Json:: " + ret);
            }
            catch (Exception ex)
            {
                WriteJournal("ASAICashInFW - ConsolidateTicketTransaction exception: " + ex.Message);
            }

           
        }


        #endregion

        #region Helpers

        private void ReadParameters()
        {
            try
            {
                string kioskParameters = string.Empty;
                JObject kioskParam;
                /////ENABLECIM1
                liveOffice.GetKioskParameters("ENABLECIM1");
                dataDict.Get(ref kioskParameters, "ASAILOFW_KIOSKPARAMETERS");
                kioskParam = JObject.Parse(kioskParameters);
                enableCIM1 = Convert.ToBoolean(kioskParam["Result"]["ENABLECIM1"]);

                /////ENABLECIM2
                liveOffice.GetKioskParameters("ENABLECIM2");
                dataDict.Get(ref kioskParameters, "ASAILOFW_KIOSKPARAMETERS");
                kioskParam = JObject.Parse(kioskParameters);
                enableCIM2 = Convert.ToBoolean(kioskParam["Result"]["ENABLECIM2"]);

                ////
                string kioskInfoParameters = "";
                JObject kioskInfoParam;
                liveOffice.GetKioskInfo("MaxTicketRedemptionAmount");
                dataDict.Get(ref kioskInfoParameters, "ASAILOFW_KIOSKINFO");
                kioskInfoParam = JObject.Parse(kioskInfoParameters);

                MAX_TICKETSTRANSACTION_VALUE = Convert.ToDecimal(kioskInfoParam["Result"]["MaxTicketRedemptionAmount"].ToString());

                kioskInfoParameters = "";
                kioskInfoParam = new JObject();
                liveOffice.GetKioskInfo("MaxTicketRedemptionCount");
                dataDict.Get(ref kioskInfoParameters, "ASAILOFW_KIOSKINFO");
                kioskInfoParam = JObject.Parse(kioskInfoParameters);

                MAX_TICKETSTRANSACTION_COUNT = Convert.ToInt32(kioskInfoParam["Result"]["MaxTicketRedemptionCount"].ToString());

            }
            catch (Exception e)
            {
                WriteJournal($"ASAICashInFW - START ASAICASHIFW_FUNC_CONNECT Exception {e.Message} ");
            }

        }
       private void  WriteJournal (string message)
       {
           dataDict.Set($"Ver. Lib:{ASAICASHINFW_VERSION}-{message}", "ASAIWELCOMETRACE");
           journal.Write(MSG500001);

       }

        private void WriteCharityJournal(string message)
        {
            dataDict.Set($"Ver. Lib:{ASAICASHINFW_VERSION}-{message}", "ASAIWELCOMETRACE");
            journal.Write(MSG400023);

        }
        Transaction GetTransactionFromDataDict()
       {
           string trx = "";
           Transaction transaction = null;

           dataDict.Get(ref trx, ASAICASHINFW_PROP_TRANSACTION);
           if (!String.IsNullOrEmpty(trx))
               transaction = DeserializeJSON(trx);

           return transaction;
       }
       private Transaction DeserializeJSON(string json)
       {
           return (JsonConvert.DeserializeObject<Transaction>(json));
       }
       private Balance DeserializeJSONBalance(string json)
       {
           return (JsonConvert.DeserializeObject<Balance>(json));
       }


        private void RaiseEvent()
        {
            short eventID = ASAICASHIFW_EVT_COMPLEX_GATEWAY_EXIT;
            WriteJournal("EVENT ASAICASHIFW_EVT_COMPLEX_GATEWAY_EXIT"); 
            FrmSendEvent(eventID);
        }

        private void RaiseEvent2(short eventID)
        {
            WriteJournal("RaiseEvent2 eventID: " + eventID);
            FrmSendEvent(eventID);
        }
        #endregion Helpers

        #region Posting
        //sAcceptorName= ContractStatusDevice.BILLACCEPTOR;
        //sAcceptorName= ContractStatusDevice.BILLACCEPTOR2;
        //status = ASAILOFW_STATUS_OK;
        //status = ASAILOFW_STATUS_ERROR;
        private void PostStatusBillAcceptor(string sAcceptorName, string status)
        {
            PostStatus poststatus = new PostStatus();
            String jsonPost = "";

            try
            {
                Task t = Task.Run(() =>
                {
                    poststatus.Device = sAcceptorName;
                    poststatus.ItemType = null;
                    poststatus.ItemDenomination = 0;
                    poststatus.ItemCount = 0;
                    poststatus.ContainerNumber = 7;
                    poststatus.Status = status;
                    poststatus.DateReported = DateTime.Now;
                    jsonPost = (Translator.SerializeJSON(poststatus));
                    dataDict.Set(jsonPost, "ASAILOFW_POSTSTATUS");
                    ASAILOFW postingLO = new ASAILOFW("");
                    postingLO.PostStatus("");

                });

            }
            catch (Exception ex)
            {
                WriteJournal("ASAICashInFW - PostStatusBillAcceptor error:" + ex.Message);
            }
        }

        //sAcceptorName = ContractStatusDevice.BILLDENOMINATION;
        //sAcceptorName = ContractStatusDevice.BILLDENOMINATION2;
        //status = ASAILOFW_STATUS_OK;
        //status = ASAILOFW_STATUS_ERROR;
        //type = "BCR";Notes
        //type = "USD";Money
        //ItemDenomination = 0; For Notes
        //ItemDenomination = 1;
        //ItemDenomination = 5;
        //ItemDenomination = 10;
        //ItemDenomination = 20;
        //ItemDenomination = 50;
        //ItemDenomination = 100;
        //ContainerNumber = 0; For Notes
        //ContainerNumber = 1; Bill $1
        //ContainerNumber = 2; Bill $5
        //ContainerNumber = 3; Bill $10
        //ContainerNumber = 4; Bill $20
        //ContainerNumber = 5; Bill $50
        //ContainerNumber = 6; Bill $100
        //count = number of bbills or notes on that denomination
        private void PostCounters()
        {
            WriteJournal("PostStatusBillDenomination");
            try
            {
                List<string> leftCounters = api.meiCmdLeft.GetCounters();
                List<string> rightCounters = api.meiCmdRight.GetCounters();

                /////Save new post into property ASAIBILLACCEPTOR_RIGHTCOUNTER - ASAIBILLACCEPTOR_LEFTCOUNTER

                dataDict.Set(String.Join("|", leftCounters), ASAIBILLACCEPTOR_PROP_LEFTCOUNTER);
                dataDict.Set(String.Join("|", rightCounters), ASAIBILLACCEPTOR_PROP_RIGHTCOUNTER);
                WriteJournal("ASAICashInFW - ASAIBILLACCEPTOR_PROP_LEFTCOUNTER - : " +  String.Join("|",leftCounters));
                WriteJournal("ASAICashInFW - ASAIBILLACCEPTOR_PROP_RIGHTCOUNTER - : " + String.Join("|",rightCounters));
                Task t = Task.Run(() =>
                {
                    PostStatus[] poststatus = new PostStatus[14];
                    short index = 0;
                    foreach (var item in leftCounters)
                    {
                        poststatus[index] = new PostStatus
                        {
                            Device = ContractStatusDevice.BILLDENOMINATION,
                            ItemType = (leftCounters[index].Split(',')[0].Trim() == "Tickets") ? "BCR" : leftCounters[index].Split(',')[1].Trim().ToUpper(),
                            ItemDenomination = (leftCounters[index].Split(',')[2].Trim() == String.Empty) ? 0 : Int32.Parse(leftCounters[index].Split(',')[2].Trim()),
                            ItemCount = Int32.Parse(leftCounters[index].Split(',')[3].Trim()),
                            ContainerNumber = (leftCounters[index].Split(',')[0].Trim() == "Tickets") ? (short)0 : (short)(index + 1),
                            DateReported = DateTime.Now,
                            Status = ASAILOFW_STATUS_OK
                        };
                        index++;
                    }
                    int leftLen = index;
                    index = 0;
                    foreach (var item in rightCounters)
                    {
                        poststatus[leftLen] = new PostStatus
                        {
                            Device = ContractStatusDevice.BILLDENOMINATION2,
                            ItemType = (rightCounters[index].Split(',')[0].Trim() == "Tickets") ? "BCR" : rightCounters[index].Split(',')[1].Trim().ToUpper(),
                            ItemDenomination = (rightCounters[index].Split(',')[2].Trim() == String.Empty) ? 0 : Int32.Parse(rightCounters[index].Split(',')[2].Trim()),
                            ItemCount = Int32.Parse(rightCounters[index].Split(',')[3].Trim()),
                            ContainerNumber = (rightCounters[index].Split(',')[0].Trim() == "Tickets") ? (short)0 : (short)(index + 1),
                            DateReported = DateTime.Now,
                            Status = ASAILOFW_STATUS_OK
                        };
                        index++;
                        leftLen++;
                    }

                    string json = JsonConvert.SerializeObject(poststatus);
                    dataDict.Set(json, "ASAILOFW_POSTSTATUS");
                    ASAILOFW asailo = new ASAILOFW("");
                    asailo.PostStatusBV(json);


            });

            }
            catch (Exception ex)
            {
                WriteJournal("ASAICashInFW -  PostStatusBillDenomination :" + ex.Message);
            }
        }

        #endregion Posting

        #region Dispenser Counters Validation

        private bool CanDispenseBills(decimal total)
        {

            bool ret = false;
            try
            {
                int amount = (int)total;
                if (amount == 0)
                {
                    WriteJournal($"CanDispenseBills TransactionTotal:{total} Coins:{amount}");
                    return true;
                }
                cdm.Open();
                CCCDMSTATUS device = new CCCDMSTATUS();
                ushort numberOfExtras = 0;
                short r = cdm.GetStatus(ref device, ref numberOfExtras);
                WriteJournal($"CanDispenseBills TransactionTotal:{total} Bills:{amount} GetStatus:{r} Device:{ device.sDevice} Shutter:{device.usShutter}");
                if (device.sDevice == CCCdmFW.CCCDMFW_DEVOK)
                {
                    CCCDMDENOMINATEDATA denominate = new CCCDMDENOMINATEDATA();
                    denominate.aulCassetteValue = new uint[CCCdmFW.CCCDM_MAX_CASSETTES];

                    for (int i = 0; i < CCCdmFW.CCCDM_MAX_CASSETTES; i++)
                    {
                        denominate.aulCassetteValue[i] = 0;
                    }
                    r = cdm.Denominate("USD", amount, ref denominate, CCCdmFW.CCCDMFW_ALGO_BIG_NOTES);
                    ret = (r == CCCdmFW.CCCDMFW_DEVOK);
                    WriteJournal($"CanDispenseBills Result of BillDenominate:{r} ret {ret}");
                    for (int i = 0; i < CCCdmFW.CCCDM_MAX_CASSETTES; i++)
                    {
                        uint dispense = denominate.aulCassetteValue[i];
                        WriteJournal($"CanDispenseBills BillDenominate[{i}] = {dispense} " );
                    }
                }
                cdm.Close();
            }
            catch (Exception e)
            {
                WriteJournal($"CanDispenseBills  TransactionTotal:{total} Exception {e.Message} {e.StackTrace}");

            }
            return ret;

        }

        private bool CanDispenseCoins(decimal total)
        {

            bool ret = false;
            try
            {
                string str = total.ToString("0.00");

                int amount = (int)total;
                int indexOfDecimal = str.IndexOf(".");
                //WriteJournal($"CanDispenseCoins TransactionTotal:{total} str:{str}");
                uint fractPart = (uint)Int32.Parse(str.Substring(indexOfDecimal + 1));
                WriteJournal($"CanDispenseCoins TransactionTotal:{total} Coins:{fractPart}");
                if (fractPart == 0)
                {
                    WriteJournal($"CanDispenseCoins TransactionTotal:{total} Coins:{fractPart}");
                    return true;
                }
                coin.Open();
                COINOUTSYSTEMSTATUS device = new COINOUTSYSTEMSTATUS();
                short r = coin.GetbStatus(ref device);
                WriteJournal($"CanDispenseCoins TransactionTotal:{total} Coins:{fractPart} GetStatus:{r} Device:{ device.usDevice}");
                if (device.usDevice == CCCoinOutFW.COINOUT_DEV_ONLINE)
                {
                    ushort numberOfCashUnits = 0;
                    coin.ReadCashUnitInfo(ref numberOfCashUnits);
                    COINOUTAMOUNT coinAmount = new COINOUTAMOUNT
                    {
                        szCurrencyID = "USD",
                        ulAmount = fractPart
                    };

                    if (HoppersInfo(numberOfCashUnits))
                    {
                        uint[] denominate = new uint[numberOfCashUnits];
                        for (int i = 0; i < numberOfCashUnits; i++)
                        {
                            denominate[i] = 0;
                        }
                        ushort algorithm = CCCoinOutFW.COINOUT_ALGO_BIG_NOTES;
                        uint mixNumber = 0;
                        r = coin.Denominate(coinAmount, ref denominate, algorithm, mixNumber);
                        ret = (r == CCCdmFW.CCCDMFW_DEVOK);
                        WriteJournal($"CanDispenseCoins Result of CoinDenominate:{r}");
                        for (int i = 0; i < numberOfCashUnits; i++)
                        {
                            WriteJournal($"CanDispenseBills CoinDenominate[{i}]:{denominate[i]}");
                        }
                    }

                }
                coin.Close();
            }
            catch (Exception e)
            {
                WriteJournal($"CanDispenseCoins TransactionTotal:{total} Exception {e.Message} {e.StackTrace}");

            }
            return ret;

        }

        private bool HoppersInfo(int numberOfHoppers)
        {
            short ret = CCCoinOutFW.COINOUT_RC_OK;
            COINOUTCASHUNIT[] hopperInfo;
            bool status1cents = false;
            bool status5cents = false;
            bool status25cents = false;
            WriteJournal($"HoppersInfo NumberOfHoppers[]:{numberOfHoppers}");

            hopperInfo = new COINOUTCASHUNIT[numberOfHoppers];
            for (int i = 0; i < numberOfHoppers; i++)
            {
                COINOUTCASHUNIT cassInfo = new COINOUTCASHUNIT();
                ret = coin.GetCashUnitInfo((ushort)i, ref cassInfo);
                hopperInfo[i] = cassInfo;
                WriteJournal($"HoppersInfo HopperInfo[{i}]:{hopperInfo[i].ulValues} Remaining:{hopperInfo[i].ulCount}");
                ushort status = hopperInfo[i].usStatus;
                if ((status == CCCoinOutFW.COINOUT_CUSTAT_OK) ||
                    (status == CCCoinOutFW.COINOUT_CUSTAT_FULL) || 
                   (status == CCCoinOutFW.COINOUT_CUSTAT_HIGH) ||
                   (status == CCCoinOutFW.COINOUT_CUSTAT_LOW) ||
                   (status == CCCoinOutFW.COINOUT_CUSTAT_EMPTY))
                {
                    if (hopperInfo[i].ulCount >  5)
                    {
                        if (hopperInfo[i].ulValues == 1)
                            status1cents = status1cents || true;
                        if (hopperInfo[i].ulValues == 5)
                            status5cents = status5cents || true;
                        if (hopperInfo[i].ulValues == 25)
                            status25cents = status25cents || true;

                    }
                }
            }
            return status1cents && status5cents  && status25cents;
        }
        #endregion

        #region Charity
        private short Charity()
        {
            string transactionStr = "";
            Transaction transaction;
            short result = 0;
            JObject kioskParam;
            string kioskParameters = String.Empty;

            try
            {
                //dataDict.Set("ASAICashInFW - Charity", "ASAIWELCOMETRACE");
                //journal.Write(500001);

                string charityName = string.Empty;
                dataDict.Get(ref charityName, "ASAICHARITY_NAME");

                if (charityName.Length > 0)
                {
                    if (charityName.Contains("NO DONATION"))
                        result = 0;
                    else
                        result = 1;  //Must be 1.  
                }
                else
                {
                    //ASAILOFW liveOffice = new ASAILOFW("");

                    dataDict.Get(ref transactionStr, ASAICASHINFW_PROP_TRANSACTION);
                    transaction = DeserializeJSON(transactionStr);

                    decimal transactionAmountInt = Math.Truncate(transaction.total);
                    decimal transactionAmountDec = transaction.total - transactionAmountInt;

                    if (transactionAmountDec > 0)
                    {
                        dataDict.Set("ASAICashInFW - Charity:  The amount has decimals", "ASAIWELCOMETRACE");
                        journal.Write(500001);

                        WriteCharityJournal("ASAICashInFW - Charity:  The amount has decimals");

                        liveOffice.GetKioskParameters("CharityEnabled");

                        dataDict.Get(ref kioskParameters, "ASAILOFW_KIOSKPARAMETERS");
                        kioskParam = JObject.Parse(kioskParameters);
                        bool charityEnabled = Convert.ToBoolean(kioskParam["Result"]["CharityEnabled"]);

                        dataDict.Set("ASAICashInFW - Charity CharityEnabled: " + charityEnabled, "ASAIWELCOMETRACE");
                        journal.Write(500001);


                        WriteCharityJournal($"ASAICashInFW - Charity CharityEnabled: {charityEnabled}");


                        if (!charityEnabled)
                        {
                            result = 0;  //Read the variable in the LiveOffice if it is a Charity Casino}
                        }
                        else
                        {
                            result = 1;//Must be 1.  
                        }
                    }
                    else
                    {
                        result = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                dataDict.Set("ASAICashInFW - Charity: " + ex.ToString(), "ASAIWELCOMETRACE");
                journal.Write(500001);
            }

            return result;
        }

        private short CharityList()
        {
            short result = 0;
            Transaction transaction;
            string transactionStr = "";
            string serverparameters = string.Empty;
            List<Charity> organizationsEnabled = new List<Charity>();

            try
            {
                //dataDict.Set("ASAICashInFW - CharityList", "ASAIWELCOMETRACE");
                //journal.Write(500001);

                CharityList charityEnableLst = new CharityList();
                List<Charity> organizationLst = new List<Charity>();

                ASAILOFW aSAILOFw = new ASAILOFW("");
                serverparameters = aSAILOFw.GetCharity("DONATION-ORG");

                dataDict.Set("ASAICashInFW - CharityList serverparameters: " + serverparameters, "ASAIWELCOMETRACE");
                journal.Write(500001);

                JObject obj = JObject.Parse(serverparameters);
                JArray organizationArray = (JArray)obj["Result"];

                for (int i = 0; i <= organizationArray.Count - 1; i++)
                {
                    Charity organization = new Charity();
                    organization.name = (string)obj["Result"][i]["ParameterName"].ToString();
                    organization.value = (string)obj["Result"][i]["ParameterValue"].ToString();

                    try
                    {
                        organization.enabled = (Boolean)obj["Result"][i]["ParameterEnabled"];
                    }
                    catch (Exception ex)
                    {
                        organization.enabled = false;
                    }

                    organization.bOServerID = (string)obj["Result"][i]["BOServerID"].ToString();
                    organization.sIcon = organization.bOServerID + ".svg";

                    dataDict.Set("ASAICashInFW - CharityList name: " + organization.name + " value: " + organization.value + " enabled: " + organization.enabled + " id: " + organization.bOServerID, "ASAIWELCOMETRACE");
                    journal.Write(500001);

                    if (organization.enabled)
                        organizationLst.Add(organization);
                }

                charityEnableLst.charityList = organizationLst;
                string charities = charityEnableLst.SerializeJSON<CharityList>(charityEnableLst);
                dataDict.Set(charities, "ASAICHARITYLIST");

                dataDict.Set("ASAICashInFW - CharityList list: " + charities, "ASAIWELCOMETRACE");
                journal.Write(500001);

                uint x = 0;
                foreach (Charity charityItem in organizationLst)
                {
                    dataDict.Set("ID" + x.ToString(), "CCTAFW_PROP_GENERIC_LIST_SELECTION_DATA", x);
                    dataDict.Set(charityItem.value, "ASAICHARITYSEL", x);
                    dataDict.Set(charityItem.sIcon, "ASAICHARITYSELICON", x);
                    x = x + 1;
                }

                //string charityName = string.Empty;
                //dataDict.Set("ID0", "CCTAFW_PROP_GENERIC_LIST_SELECTION_DATA", 0);
                //dataDict.Set("Help All Children", "ASAICHARITYSEL", 0);
                //dataDict.Set("ID1", "CCTAFW_PROP_GENERIC_LIST_SELECTION_DATA", 1);
                //dataDict.Set("Peace Makes Leaders", "ASAICHARITYSEL", 1);
                //dataDict.Set("ID2", "CCTAFW_PROP_GENERIC_LIST_SELECTION_DATA", 2);
                //dataDict.Set("Preserve Earth", "ASAICHARITYSEL", 2);                
                //dataDict.Set("No Donation", "ASAICHARITYSEL", i);
                //dataDict.Set("ID99", "CCTAFW_PROP_GENERIC_LIST_SELECTION_DATA", i);    

                dataDict.Set("ID99", "CCTAFW_PROP_GENERIC_LIST_SELECTION_DATA", x);

                dataDict.Get(ref transactionStr, ASAICASHINFW_PROP_TRANSACTION);
                transaction = DeserializeJSON(transactionStr);

                decimal transactionAmountInt = Math.Truncate(transaction.total);
                decimal transactionAmountDec = transaction.total - transactionAmountInt;

                String formtted = string.Format("{0:0.00}", transactionAmountDec);

                dataDict.Set("ASAICashInFW - CharityList cash: " + transactionAmountInt.ToString() + " change: " + formtted, "ASAIWELCOMETRACE");
                journal.Write(500001);

                dataDict.Set(transactionAmountInt.ToString(), "ASAICHARITY_CASH");
                dataDict.Set(formtted, "ASAICHARITY_CHANGE");
            }
            catch (Exception ex)
            {
                dataDict.Set("ASAICashInFW - CharityList: " + ex.ToString(), "ASAIWELCOMETRACE");
                journal.Write(500001);
            }

            return result;
        }

        private short CharityProcess()
        {
            short result = 0;
            Transaction transaction;
            string transactionStr = "";
            string change_end = string.Empty;

            try
            {
                //dataDict.Set("ASAICashInFW - CharityProcess", "ASAIWELCOMETRACE");
                //journal.Write(500001);

                string selection = String.Empty;
                dataDict.Get(ref selection, "CCTAFW_PROP_UI_VIEW_INTERACTION_RESULT");

                //dataDict.Set("ASAICashInFW - CharityProcess selection: " + selection, "ASAIWELCOMETRACE");
                //journal.Write(500001);

                if (!selection.Contains("CANCEL") && selection.Length > 0)
                {
                    int id = Convert.ToInt32(selection.Remove(0, 2));

                    dataDict.Set("ASAICashInFW - CharityProcess ID: " + id, "ASAIWELCOMETRACE");
                    journal.Write(500001);

                    if (id != 99)
                    {
                        dataDict.Get(ref transactionStr, ASAICASHINFW_PROP_TRANSACTION);
                        transaction = DeserializeJSON(transactionStr);

                        decimal transactionAmountInt = Math.Truncate(transaction.total);
                        decimal transactionAmountDec = transaction.total - transactionAmountInt;
                        String formtted = string.Format("{0:0.00}", transactionAmountDec);

                        dataDict.Set("ASAICashInFW - CharityProcess: Integer value: " + transactionAmountInt + " Decimal value: " + transactionAmountDec, "ASAIWELCOMETRACE");
                        journal.Write(500001);                      

                        dataDict.Set(transactionAmountDec.ToString(), "ASAICHARITY_CHANGE_END");
                        dataDict.Set(transactionAmountInt.ToString(), "ASAICHARITY_CASH");
                        dataDict.Set(formtted, "ASAICHARITY_CHANGE");

                        dataDict.Get(ref change_end, "ASAICHARITY_CHANGE_END"); //para validar toca borrar
                        dataDict.Set("ASAICashInFW - CharityProcess change_end: " + change_end, "ASAIWELCOMETRACE");
                        journal.Write(500001);

                        //Charity
                        string charityName = string.Empty;
                        string charityStrList = string.Empty;
                        string charityID = string.Empty;
                        string charityValue = string.Empty;
                        CharityList charityList = new CharityList();
                        List<Charity> charitiesLst = new List<Charity>();

                        uint i = (uint)id;
                        dataDict.Get(ref charityName, "ASAICHARITYSEL", i);

                        dataDict.Set("ASAICashInFW - CharityProcess charityName: " + charityName, "ASAIWELCOMETRACE");
                        journal.Write(500001);

                        dataDict.Get(ref charityStrList, "ASAICHARITYLIST");
                        charityList = charityList.DeserializeJSON<CharityList>(charityStrList);
                        charitiesLst = charityList.charityList;

                        dataDict.Set("ASAICashInFW - CharityProcess charityStrList: " + charityStrList, "ASAIWELCOMETRACE");
                        journal.Write(500001);

                        foreach (Charity item in charitiesLst)
                        {
                            if (item.value.Contains(charityName))
                            {
                                //dataDict.Set("ASAICashInFW - CharityProcess -  item.value: " + item.value + " item.bOServerID: " + item.bOServerID, "ASAIWELCOMETRACE");
                                //journal.Write(500001);

                                charityID = item.bOServerID;
                                charityValue = item.value;
                            }
                        }

                        dataDict.Set(string.Empty, "ASAICHARITYLIST"); //Clean
                        dataDict.Set(charityName, "ASAICHARITY_NAME");
                        dataDict.Set(charityID, "ASAICHARITYID");
                        dataDict.Set(charityValue, "ASAICHARITY_VALUE");

                        dataDict.Set("ASAICashInFW - CharityProcess change_end2: " + change_end, "ASAIWELCOMETRACE");
                        journal.Write(500001);
                    }
                    else
                    {
                        dataDict.Set(string.Empty, "ASAICHARITY_CASH");
                        dataDict.Set(string.Empty, "ASAICHARITY_CHANGE");
                        dataDict.Set("NO DONATION", "ASAICHARITY_NAME");
                        dataDict.Set(string.Empty, "ASAICHARITYLIST");
                        dataDict.Set(string.Empty, "ASAICHARITY_CHANGE_END");
                        dataDict.Set(string.Empty, "ASAICHARITYID");
                        dataDict.Set(string.Empty, "ASAICHARITY_VALUE");
                    }
                }
                else
                {
                    dataDict.Set(string.Empty, "ASAICHARITY_CASH");
                    dataDict.Set(string.Empty, "ASAICHARITY_CHANGE");
                    dataDict.Set("NO DONATION", "ASAICHARITY_NAME");
                    dataDict.Set(string.Empty, "ASAICHARITYLIST");
                    dataDict.Set(string.Empty, "ASAICHARITY_CHANGE_END");
                    dataDict.Set(string.Empty, "ASAICHARITYID");
                    dataDict.Set(string.Empty, "ASAICHARITY_VALUE");
                }
            }
            catch (Exception ex)
            {
                dataDict.Set("ASAICashInFW - CharityProcess Error: " + ex.ToString(), "ASAIWELCOMETRACE");
                journal.Write(500001);

                dataDict.Set(string.Empty, "ASAICHARITY_CASH");
                dataDict.Set(string.Empty, "ASAICHARITY_CHANGE");
                dataDict.Set("NO DONATION", "ASAICHARITY_NAME");
                dataDict.Set(string.Empty, "ASAICHARITYLIST");
                dataDict.Set(string.Empty, "ASAICHARITY_CHANGE_END");
                dataDict.Set(string.Empty, "ASAICHARITYID");
                dataDict.Set(string.Empty, "ASAICHARITY_VALUE");
            }

            //dataDict.Set("ASAICashInFW - CharityProcess change_end3: " + change_end, "ASAIWELCOMETRACE");
            //journal.Write(500001);

            return result;
        }

        private short CharityProcessSelect()
        {
            short result = 0;
            //0 = Dispensing
            //1 = CharityConfirmNO
            //2 = CharityConfirmYES
            Transaction transaction;
            string transactionStr = "";
            string change_end = string.Empty;

            try
            {
                //dataDict.Set("ASAICashInFW - CharityProcess", "ASAIWELCOMETRACE");
                //journal.Write(500001);

                string selection = String.Empty;
                dataDict.Get(ref selection, "CCTAFW_PROP_UI_VIEW_INTERACTION_RESULT");

                //dataDict.Set("ASAICashInFW - CharityProcess selection: " + selection, "ASAIWELCOMETRACE");
                //journal.Write(500001);

                if (!selection.Contains("CANCEL") && selection.Length > 0)
                {
                    int id = Convert.ToInt32(selection.Remove(0, 2));

                    dataDict.Set("ASAICashInFW - CharityProcess ID: " + id, "ASAIWELCOMETRACE");
                    journal.Write(500001);

                    if (id != 99)
                    {
                        dataDict.Get(ref transactionStr, ASAICASHINFW_PROP_TRANSACTION);
                        transaction = DeserializeJSON(transactionStr);

                        decimal transactionAmountInt = Math.Truncate(transaction.total);
                        decimal transactionAmountDec = transaction.total - transactionAmountInt;
                        String formtted = string.Format("{0:0.00}", transactionAmountDec);

                        dataDict.Set("ASAICashInFW - CharityProcess: Integer value: " + transactionAmountInt + " Decimal value: " + transactionAmountDec, "ASAIWELCOMETRACE");
                        journal.Write(500001);

                        dataDict.Set(transactionAmountDec.ToString(), "ASAICHARITY_CHANGE_END");
                        dataDict.Set(transactionAmountInt.ToString(), "ASAICHARITY_CASH");
                        dataDict.Set(formtted, "ASAICHARITY_CHANGE");

                        dataDict.Get(ref change_end, "ASAICHARITY_CHANGE_END"); //para validar toca borrar
                        dataDict.Set("ASAICashInFW - CharityProcess change_end: " + change_end, "ASAIWELCOMETRACE");
                        journal.Write(500001);

                        //Charity
                        string charityName = string.Empty;
                        string charityStrList = string.Empty;
                        string charityID = string.Empty;
                        string charityValue = string.Empty;
                        CharityList charityList = new CharityList();
                        List<Charity> charitiesLst = new List<Charity>();

                        uint i = (uint)id;
                        dataDict.Get(ref charityName, "ASAICHARITYSEL", i);

                        dataDict.Set("ASAICashInFW - CharityProcess charityName: " + charityName, "ASAIWELCOMETRACE");
                        journal.Write(500001);

                        dataDict.Get(ref charityStrList, "ASAICHARITYLIST");
                        charityList = charityList.DeserializeJSON<CharityList>(charityStrList);
                        charitiesLst = charityList.charityList;

                        dataDict.Set("ASAICashInFW - CharityProcess charityStrList: " + charityStrList, "ASAIWELCOMETRACE");
                        journal.Write(500001);

                        foreach (Charity item in charitiesLst)
                        {
                            if (item.value.Contains(charityName))
                            {
                                //dataDict.Set("ASAICashInFW - CharityProcess -  item.value: " + item.value + " item.bOServerID: " + item.bOServerID, "ASAIWELCOMETRACE");
                                //journal.Write(500001);

                                charityID = item.bOServerID;
                                charityValue = item.value;
                            }
                        }

                        dataDict.Set(string.Empty, "ASAICHARITYLIST"); //Clean
                        dataDict.Set(charityName, "ASAICHARITY_NAME");
                        dataDict.Set(charityID, "ASAICHARITYID");
                        dataDict.Set(charityValue, "ASAICHARITY_VALUE");

                        dataDict.Set("ASAICashInFW - CharityProcess change_end2: " + change_end, "ASAIWELCOMETRACE");
                        journal.Write(500001);

                        //Show ASAICharityDisplayConfirmYES
                        //dataDict.Set(string.Empty, "ASAICHARITYYES_CASH");
                        //dataDict.Set(string.Empty, "ASAICHARITYYES_CHANGE");
                        result = 2;
                    }
                    else
                    {
                        //The user selected NO DONATION Button
                        dataDict.Set(string.Empty, "ASAICHARITY_CASH");
                        dataDict.Set(string.Empty, "ASAICHARITY_CHANGE");
                        dataDict.Set("NO DONATION", "ASAICHARITY_NAME");
                        dataDict.Set(string.Empty, "ASAICHARITYLIST");
                        dataDict.Set(string.Empty, "ASAICHARITY_CHANGE_END");
                        dataDict.Set(string.Empty, "ASAICHARITYID");
                        dataDict.Set(string.Empty, "ASAICHARITY_VALUE");

                        //Show ASAICharityDisplayConfirmNO
                        dataDict.Set(string.Empty, "ASAICHARITYNO_CASH");
                        dataDict.Set(string.Empty, "ASAICHARITYNO_CHANGE");
                        result = 1;
                    }
                }
                else
                {
                    //The user selected CANCEL Button or the selection is empty
                    dataDict.Set(string.Empty, "ASAICHARITY_CASH");
                    dataDict.Set(string.Empty, "ASAICHARITY_CHANGE");
                    dataDict.Set("NO DONATION", "ASAICHARITY_NAME");
                    dataDict.Set(string.Empty, "ASAICHARITYLIST");
                    dataDict.Set(string.Empty, "ASAICHARITY_CHANGE_END");
                    dataDict.Set(string.Empty, "ASAICHARITYID");
                    dataDict.Set(string.Empty, "ASAICHARITY_VALUE");

                    //Show ASAICharityDisplayConfirmNO
                    dataDict.Set(string.Empty, "ASAICHARITYNO_CASH");
                    dataDict.Set(string.Empty, "ASAICHARITYNO_CHANGE");
                    result = 1;
                }
            }
            catch (Exception ex)
            {
                dataDict.Set("ASAICashInFW - CharityProcess Error: " + ex.ToString(), "ASAIWELCOMETRACE");
                journal.Write(500001);

                dataDict.Set(string.Empty, "ASAICHARITY_CASH");
                dataDict.Set(string.Empty, "ASAICHARITY_CHANGE");
                dataDict.Set("NO DONATION", "ASAICHARITY_NAME");
                dataDict.Set(string.Empty, "ASAICHARITYLIST");
                dataDict.Set(string.Empty, "ASAICHARITY_CHANGE_END");
                dataDict.Set(string.Empty, "ASAICHARITYID");
                dataDict.Set(string.Empty, "ASAICHARITY_VALUE");
            }

            //dataDict.Set("ASAICashInFW - CharityProcess change_end3: " + change_end, "ASAIWELCOMETRACE");
            //journal.Write(500001);

            return result;
        }

        private short CharityConfirm()
        {
            short result = 0;

            try
            {
                dataDict.Set("ASAICashInFW - CharityConfirm", "ASAIWELCOMETRACE");
                journal.Write(500001);

                //Get Selection button result
                string selection = String.Empty;
                dataDict.Get(ref selection, "CCTAFW_PROP_UI_VIEW_INTERACTION_RESULT");

                //dataDict.Set("ASAICashInFW - CharityProcess selection: " + selection, "ASAIWELCOMETRACE");
                //journal.Write(500001);

                if (!selection.Contains("CANCEL") && selection.Length > 0)
                {
                    //User selected YES or NO button
                    if (selection.Contains("YES")) {
                        //YES Button was selected
                        dataDict.Set("ASAICashInFW - CharityConfirm: YES was selected", "ASAIWELCOMETRACE");
                        journal.Write(500001);
                        //Go to Dispensing Process
                    }
                    else
                    {
                        //NO Button was selected
                        dataDict.Set("ASAICashInFW - CharityConfirm: NO was selected", "ASAIWELCOMETRACE");
                        journal.Write(500001);
                        //Clean the Charity variables
                        dataDict.Set(string.Empty, "ASAICHARITY_CASH");
                        dataDict.Set(string.Empty, "ASAICHARITY_CHANGE");
                        dataDict.Set("NO DONATION", "ASAICHARITY_NAME");
                        dataDict.Set(string.Empty, "ASAICHARITYLIST");
                        dataDict.Set(string.Empty, "ASAICHARITY_CHANGE_END");
                        dataDict.Set(string.Empty, "ASAICHARITYID");
                        dataDict.Set(string.Empty, "ASAICHARITY_VALUE");
                    }
                 }
                else
                {
                    //Contains CANCEL or is empty
                    dataDict.Set("ASAICashInFW - CharityConfirm: CANCEL was selected", "ASAIWELCOMETRACE");
                    journal.Write(500001);
                    //Clean the Charity variables
                    dataDict.Set(string.Empty, "ASAICHARITY_CASH");
                    dataDict.Set(string.Empty, "ASAICHARITY_CHANGE");
                    dataDict.Set("NO DONATION", "ASAICHARITY_NAME");
                    dataDict.Set(string.Empty, "ASAICHARITYLIST");
                    dataDict.Set(string.Empty, "ASAICHARITY_CHANGE_END");
                    dataDict.Set(string.Empty, "ASAICHARITYID");
                    dataDict.Set(string.Empty, "ASAICHARITY_VALUE");
                }
            }
            catch (Exception ex)
            {
                dataDict.Set("ASAICashInFW - CharityProcess Error: " + ex.ToString(), "ASAIWELCOMETRACE");
                journal.Write(500001);

                //An error occurred, clean the Charity variables

                //Clean the Charity variables
                dataDict.Set(string.Empty, "ASAICHARITY_CASH");
                dataDict.Set(string.Empty, "ASAICHARITY_CHANGE");
                dataDict.Set("NO DONATION", "ASAICHARITY_NAME");
                dataDict.Set(string.Empty, "ASAICHARITYLIST");
                dataDict.Set(string.Empty, "ASAICHARITY_CHANGE_END");
                dataDict.Set(string.Empty, "ASAICHARITYID");
                dataDict.Set(string.Empty, "ASAICHARITY_VALUE");
            }

            return result;
        }
        #endregion others        

        #region SMS
        private bool StartUp()
        {
            bool result = false; //By default is an error;

            RequestMsgFactory request = new RequestMsgFactory();
            ResponseMsgFactory response = new ResponseMsgFactory();

            try
            {
                NetNamedPipeBinding netNamedPipeBinding = new NetNamedPipeBinding();
                EndpointAddress endPointAddress = new EndpointAddress(@"net.pipe://localhost/TicketRedemptionService");
                ChannelFactory<ITicketRedemptionFactory> channelFactory = new ChannelFactory<ITicketRedemptionFactory>(netNamedPipeBinding, endPointAddress);
                ITicketRedemptionFactory proxyObject = channelFactory.CreateChannel();

                WriteJournal("ASAICashInFW - StartUp");
                response = proxyObject.StartUp(request);
                 WriteJournal("ASAICashInFW - StartUp-Response. Code: " + response.ResponseCode + " Description: " + response.ResponseDescription);
                if (Convert.ToInt32(response.ResponseCode) == 0)
                    result = true;
            }
            catch (Exception e)
            {                
                WriteJournal("ASAICashInFW - StartUp - Error: " + e.Message + " StackTrace: " + e.StackTrace);              
            }
            return result;
        }

        private decimal Validate(string transactionID, string ticketNumber)
        {
            decimal amount = 0;
            string ret = string.Empty;
            //decimal maxAmountTRTrx = 1000; //maxAmountTicket = 0;

            RequestMsgFactory request = new RequestMsgFactory();
            ResponseMsgFactory response = new ResponseMsgFactory();

            request.TransactionID = transactionID;
            request.Amount = 0;
            request.TicketNumber = ticketNumber;

            try
            {
                NetNamedPipeBinding netNamedPipeBinding = new NetNamedPipeBinding();
                EndpointAddress endPointAddress = new EndpointAddress(@"net.pipe://localhost/TicketRedemptionService");
                ChannelFactory<ITicketRedemptionFactory> channelFactory = new ChannelFactory<ITicketRedemptionFactory>(netNamedPipeBinding, endPointAddress);
                ITicketRedemptionFactory proxyObject = channelFactory.CreateChannel();

                WriteJournal("ASAICashInFW - Validate. TransactionID: " + transactionID + " TicketNumber: " + ticketNumber);
             
                response = proxyObject.ValidateTicket(request);

                WriteJournal("ASAICashInFW - Validate-Response. Code: " + response.ResponseCode + " Description: " + response.ResponseDescription + " Amount: " + response.Amount.ToString());
             
                if (Convert.ToInt32(response.ResponseCode) == 0)
                {
                    amount = response.Amount;
                }
                else
                {
                    amount = 0;
                }
            }
            catch (Exception e)
            {                
                WriteJournal("ASAICashInFW - Validate - Error: " + e.Message + " StackTrace: " + e.StackTrace);
            }
            return amount;
        }

        private bool Redeem(string transactionID, decimal amount, string ticketNumber)
        {
            bool result = false; //By default is an error;

            RequestMsgFactory request = new RequestMsgFactory();
            ResponseMsgFactory response = new ResponseMsgFactory();

            request.TransactionID = transactionID;
            request.Amount = amount;
            request.TicketNumber = ticketNumber;

            try
            {
                //int id = redeemTickets.IndexOf(ticketNumber);

                //if (id <= 0)
                //{
                    NetNamedPipeBinding netNamedPipeBinding = new NetNamedPipeBinding();
                    EndpointAddress endPointAddress = new EndpointAddress(@"net.pipe://localhost/TicketRedemptionService");
                    ChannelFactory<ITicketRedemptionFactory> channelFactory = new ChannelFactory<ITicketRedemptionFactory>(netNamedPipeBinding, endPointAddress);
                    ITicketRedemptionFactory proxyObject = channelFactory.CreateChannel();

                   
                    WriteJournal("ASAICashInFW - Redeem. TransactionID: " + transactionID + " TicketNumber: " + ticketNumber + " Amount: " + amount);
                
                    response = proxyObject.RedemptionTicket(request);
                 

                    WriteJournal("ASAICashInFW - Redeem-Response. Code: " + response.ResponseCode + " Description: " + response.ResponseDescription + " Amount: " + response.Amount.ToString());

                    if (Convert.ToInt32(response.ResponseCode) == 0)
                        result = true;
                    else
                        result = false;

                //}
            }
            catch (Exception e)
            {                
                WriteJournal("ASAICashInFW - Redeem - Error: " + e.Message + " StackTrace: " + e.StackTrace);
            }
            return result;
        }

        private bool Cancel(string transactionID, decimal amount, string ticketNumber)
        {
            bool result = false; //By default all is fine, because we return the ticket. Only is import the result when is neccesary to dispense the previos tickets;

            RequestMsgFactory request = new RequestMsgFactory();
            ResponseMsgFactory response = new ResponseMsgFactory();

            request.TransactionID = transactionID;
            request.Amount = amount;
            request.TicketNumber = ticketNumber;

            try
            {
                NetNamedPipeBinding netNamedPipeBinding = new NetNamedPipeBinding();
                EndpointAddress endPointAddress = new EndpointAddress(@"net.pipe://localhost/TicketRedemptionService");
                ChannelFactory<ITicketRedemptionFactory> channelFactory = new ChannelFactory<ITicketRedemptionFactory>(netNamedPipeBinding, endPointAddress);
                ITicketRedemptionFactory proxyObject = channelFactory.CreateChannel();

                WriteJournal("ASAICashInFW - Cancel. TransactionID: " + transactionID + " TicketNumber: " + ticketNumber + " Amount: " + amount);
                response = proxyObject.CancelTicket(request);
                WriteJournal("ASAICashInFW - Cancel-Response. Code: " + response.ResponseCode + " Description: " + response.ResponseDescription + " Amount: " + response.Amount.ToString());
             
                if (Convert.ToInt32(response.ResponseCode) == 0)
                    result = true;
                else
                    result = false;
            }
            catch (Exception e)
            {               
                WriteJournal("ASAICashInFW - Cancel - Error: " + e.Message + " StackTrace: " + e.StackTrace);
            }
            return result;
        }

        #endregion SMS

        #region SOP
        private short ResetLeft()
        {
            string status = "";

            try
            {
                WriteJournal("ASAICashInFW - SOP - ResetLeft");
               
                CCSopDialogFW sop3 = new CCSopDialogFW();
                short auxTemp3 = sop3.DoDialog("ASAI_WAIT", "PROCESSING....");

                api.meiCmdLeft.SoftReset();

                string result = "The device was reset.";

                RegistryKey myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_RESULT", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", result, RegistryValueKind.String);
                    myKey.Close();
                }
            }
            catch (Exception ex)
            {
                WriteJournal("ASAICashInFW - SOP - ResetLeft error: " + ex.Message);

                RegistryKey myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_RESULT", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", status + ex.Message, RegistryValueKind.String);
                    myKey.Close();
                }
            }
            return 0;
        }

        private short ResetRight()
        {
            string status = "";

            try
            {
                WriteJournal("ASAICashInFW - SOP - ResetRight");

                CCSopDialogFW sop3 = new CCSopDialogFW();
                short auxTemp3 = sop3.DoDialog("ASAI_WAIT", "PROCESSING....");

                api.meiCmdRight.SoftReset();

                string result = "The device was reset.";

                RegistryKey myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_RESULT", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", result, RegistryValueKind.String);
                    myKey.Close();
                }
            }
            catch (Exception ex)
            {
                WriteJournal("ASAICashInFW - SOP - ResetRight error: " + ex.Message);

                RegistryKey myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_RESULT", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", status + ex.Message, RegistryValueKind.String);
                    myKey.Close();
                }
            }
            return 0;
        }

        private short CountersLeft()
        {
            string status = "Error. ";
            string usdOne = "N/A";
            string usdFive = "N/A";
            string usdTen = "N/A";
            string usdTwenty = "N/A";
            string usdFifty = "N/A";
            string usdOneHoundred = "N/A";
            string notes = "N/A";

            var countersLeft = new List<string>();

            try
            {
                WriteJournal("ASAICashInFW - SOP - CountersLeft");

                CCSopDialogFW sop3 = new CCSopDialogFW();
                short auxTemp3 = sop3.DoDialog("ASAI_WAIT", "PROCESSING....");

                countersLeft = api.meiCmdLeft.GetCounters();

                foreach(string s in countersLeft)
                {
                    WriteJournal("ASAICashInFW - SOP - CountersLeft s: " + s);
                }

                string one = countersLeft[0].ToString();
                string five = countersLeft[1].ToString();
                string ten = countersLeft[2].ToString();
                string twenty = countersLeft[3].ToString();
                string fifty = countersLeft[4].ToString();
                string hundred = countersLeft[5].ToString();
                string notesNum = countersLeft[6].ToString();

                String[] oneList = one.Split(',');
                String[] fiveList = five.Split(',');
                String[] tenList = ten.Split(',');
                String[] twentyList = twenty.Split(',');
                String[] fiftyList = fifty.Split(',');
                String[] hundredList = hundred.Split(',');
                String[] notesList = notesNum.Split(',');

                usdOne = oneList[3].Trim();
                usdFive = fiveList[3].Trim();
                usdTen = tenList[3].Trim();
                usdTwenty = twentyList[3].Trim();
                usdFifty = fiftyList[3].Trim();
                usdOneHoundred = hundredList[3].Trim();
                notes = notesList[3].Trim();

                WriteJournal("ASAICashInFW - SOP - CountersLeft den 1: " + usdOne + " den 5: " + usdFive + " den 10: " + usdTen + " den 20: " + usdTwenty + " den 50: " + usdFifty + " den 100: " + usdOneHoundred + " tickets: " + notes);

                status = "The BV1 counters are: ";

                RegistryKey myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_RESULT", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", status, RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $1
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB1_COUNTERS1", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $1   Count: " + usdOne, RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $5   
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB1_COUNTERS2", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $5   Count: " + usdFive, RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $10      
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB1_COUNTERS3", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $10  Count: " + usdTen, RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $20            
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB1_COUNTERS4", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $20  Count: " + usdTwenty, RegistryValueKind.String);
                    myKey.Close();
                }

                //USD $50
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB1_COUNTERS5", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $50  Count: " + usdFifty, RegistryValueKind.String);
                    myKey.Close();
                }

                //USD $100
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB1_COUNTERS6", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $100 Count: " + usdOneHoundred, RegistryValueKind.String);
                    myKey.Close();
                }

                //Tickets
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB1_COUNTERS7", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "Tickets. Count: " + notes, RegistryValueKind.String);
                    myKey.Close();
                }
            }
            catch (Exception ex)
            {
                status = "Connection Error. Counters BB1: ";

                RegistryKey myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_RESULT", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", status, RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $1
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB1_COUNTERS1", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $1   Count: " + "N/A", RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $5   
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB1_COUNTERS2", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $5   Count: " + "N/A", RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $10      
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB1_COUNTERS3", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $10  Count: " + "N/A", RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $20            
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB1_COUNTERS4", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $20  Count: " + "N/A", RegistryValueKind.String);
                    myKey.Close();
                }

                //USD $50
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB1_COUNTERS5", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $50  Count: " + "N/A", RegistryValueKind.String);
                    myKey.Close();
                }

                //USD $100
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB1_COUNTERS6", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $100 Count: " + "N/A", RegistryValueKind.String);
                    myKey.Close();
                }

                //Tickets
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB1_COUNTERS7", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "Tickets. Count: " + "N/A", RegistryValueKind.String);
                    myKey.Close();
                }

                WriteJournal("ASAICashInFW - SOP - CountersLeft Error: " + ex.Message);
            }
            return 0;
        }

        private short CountersRight()
        {
            string status = "Error. ";
            string usdOne = "N/A";
            string usdFive = "N/A";
            string usdTen = "N/A";
            string usdTwenty = "N/A";
            string usdFifty = "N/A";
            string usdOneHoundred = "N/A";
            string notes = "N/A";

            var countersRight = new List<string>();

            try
            {
                WriteJournal("ASAICashInFW - SOP - CountersRight");

                CCSopDialogFW sop3 = new CCSopDialogFW();
                short auxTemp3 = sop3.DoDialog("ASAI_WAIT", "PROCESSING....");

                countersRight = api.meiCmdRight.GetCounters();

                foreach (string s in countersRight)
                {
                    WriteJournal("ASAICashInFW - SOP -countersRigh s: " + s);
                }

                string one = countersRight[0].ToString();
                string five = countersRight[1].ToString();
                string ten = countersRight[2].ToString();
                string twenty = countersRight[3].ToString();
                string fifty = countersRight[4].ToString();
                string hundred = countersRight[5].ToString();
                string notesNum = countersRight[6].ToString();

                String[] oneList = one.Split(',');
                String[] fiveList = five.Split(',');
                String[] tenList = ten.Split(',');
                String[] twentyList = twenty.Split(',');
                String[] fiftyList = fifty.Split(',');
                String[] hundredList = hundred.Split(',');
                String[] notesList = notesNum.Split(',');

                usdOne = oneList[3].Trim();
                usdFive = fiveList[3].Trim();
                usdTen = tenList[3].Trim();
                usdTwenty = twentyList[3].Trim();
                usdFifty = fiftyList[3].Trim();
                usdOneHoundred = hundredList[3].Trim();
                notes = notesList[3].Trim();

                WriteJournal("ASAICashInFW - SOP - CountersRight den 1: " + usdOne + " den 5: " + usdFive + " den 10: " + usdTen + " den 20: " + usdTwenty + " den 50: " + usdFifty + " den 100: " + usdOneHoundred + " tickets: " + notes);

                status = "The BV2 counters are: ";

                RegistryKey myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_RESULT", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", status, RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $1
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB2_COUNTERS1", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $1   Count: " + usdOne, RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $5   
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB2_COUNTERS2", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $5   Count: " + usdFive, RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $10      
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB2_COUNTERS3", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $10  Count: " + usdTen, RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $20            
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB2_COUNTERS4", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $20  Count: " + usdTwenty, RegistryValueKind.String);
                    myKey.Close();
                }

                //USD $50
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB2_COUNTERS5", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $50  Count: " + usdFifty, RegistryValueKind.String);
                    myKey.Close();
                }

                //USD $100
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB2_COUNTERS6", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $100 Count: " + usdOneHoundred, RegistryValueKind.String);
                    myKey.Close();
                }

                //Tickets
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB1_COUNTERS7", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "Tickets. Count: " + notes, RegistryValueKind.String);
                    myKey.Close();
                }
            }
            catch (Exception ex)
            {
                status = "Connection Error. Counters BB1: ";

                RegistryKey myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_RESULT", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", status, RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $1
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB2_COUNTERS1", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $1   Count: " + "N/A", RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $5   
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB2_COUNTERS2", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $5   Count: " + "N/A", RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $10      
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB2_COUNTERS3", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $10  Count: " + "N/A", RegistryValueKind.String);
                    myKey.Close();
                }

                //"USD $20            
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB2_COUNTERS4", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $20  Count: " + "N/A", RegistryValueKind.String);
                    myKey.Close();
                }

                //USD $50
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB2_COUNTERS5", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $50  Count: " + "N/A", RegistryValueKind.String);
                    myKey.Close();
                }

                //USD $100
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB2_COUNTERS6", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "USD $100 Count: " + "N/A", RegistryValueKind.String);
                    myKey.Close();
                }

                //Tickets
                myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_BB2_COUNTERS7", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", "Tickets. Count: " + "N/A", RegistryValueKind.String);
                    myKey.Close();
                }

                WriteJournal("ASAICashInFW - SOP - CountersLeft Error: " + ex.Message);
            }
            return 0;
        }

        private short GetStatusLeft()
        {
            string status = "Error: ";
            string result = "The device is disconnected.";

            try
            {
                WriteJournal("ASAICashInFW - SOP - GetStatusLeft");
                CCSopDialogFW sop3 = new CCSopDialogFW();
                short auxTemp3 = sop3.DoDialog("ASAI_WAIT", "PROCESSING....");

                MEI_BV_Devices.MeiEventArgs args = new MEI_BV_Devices.MeiEventArgs();

                args = api.meiCmdLeft.GetStatus();

                WriteJournal("ASAICashInFW - SOP - GetStatusLeft STATE: " + args.deviceArgs.state);

                status = "State: ";
                result = args.deviceArgs.state;

                RegistryKey myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_RESULT", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", status + result, RegistryValueKind.String);
                    myKey.Close();
                }

            }
            catch (Exception ex)
            {
                RegistryKey myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_RESULT", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", status + ex.Message, RegistryValueKind.String);
                    myKey.Close();
                }

                WriteJournal("ASAICashInFW - SOP - GetStatusLeft Error: " + ex.Message);
            }

            return 0;
        }

        private short GetStatusRight()
        {
            string status = "Error: ";
            string result = "The device is disconnected.";

            try
            {
                WriteJournal("ASAICashInFW - SOP - GetStatusRight");
                CCSopDialogFW sop3 = new CCSopDialogFW();
                short auxTemp3 = sop3.DoDialog("ASAI_WAIT", "PROCESSING....");

                MEI_BV_Devices.MeiEventArgs args = new MEI_BV_Devices.MeiEventArgs();

                args = api.meiCmdRight.GetStatus();

                WriteJournal("ASAICashInFW - SOP - GetStatusRight STATE: " + args.deviceArgs.state);

                status = "State: ";
                result = args.deviceArgs.state;

                RegistryKey myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_RESULT", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", status + result, RegistryValueKind.String);
                    myKey.Close();
                }

            }
            catch (Exception ex)
            {
                RegistryKey myKey = Registry.LocalMachine.OpenSubKey(regeditAddress + "#DIA_DIAGNOSTIC_MENU_RESULT", true);
                if (myKey != null)
                {
                    myKey.SetValue("STRING", status + ex.Message, RegistryValueKind.String);
                    myKey.Close();
                }

                WriteJournal("ASAICashInFW - SOP - GetStatusRight Error: " + ex.Message);
            }

            return 0;
        }

        private short GetBvInfoLeft()
        {
            StringBuilder result = new StringBuilder();
            string resultFlag = string.Empty;

            try
            {
                WriteJournal("ASAICashInFW - SOP - GetBvInfoLeft");

                MEI_BV_Devices.MeiEventArgs args = new MEI_BV_Devices.MeiEventArgs();

                args = api.meiCmdLeft.GetStatus();

                resultFlag = "Port:                   " + args.configArgs.port + "\n";
                result.Append(resultFlag);
                resultFlag = "Position:               " + args.configArgs.position + "\n";
                result.Append(resultFlag);
                resultFlag = "Id:                     " + args.configArgs.id + "\n";
                result.Append(resultFlag);
                resultFlag = "Autoconnect:            " + args.configArgs.autoConnect + "\n";
                result.Append(resultFlag);
                resultFlag = "Autostack:              " + args.configArgs.autoStack + "\n";
                result.Append(resultFlag);
                resultFlag = "Barcodes enabled:       " + args.configArgs.enableBarcodes + "\n";
                result.Append(resultFlag);
                resultFlag = "PUP escrow action:      " + args.configArgs.pupEscrowAction + "\n";
                result.Append(resultFlag);
                resultFlag = "Escrow bill timeout:    " + args.configArgs.billReturnTimer + "\n";
                result.Append(resultFlag);
                resultFlag = "Escrow barcode timeout: " + args.configArgs.barcodeReturnTimer + "\n";
                result.Append(resultFlag);
                resultFlag = "Credit on stack:        " + args.configArgs.autoCreditOnStack + "\n";
                result.Append(resultFlag);

                result.Append("OK\n");

                CCSopDialogFW sop = new CCSopDialogFW();            
                short aux = sop.DoDialog("ASAI_BB1_INFO_RESULT", result.ToString());

                WriteJournal("ASAICashInFW - SOP - GetBvInfoLeft: " + result.ToString());               
            }
            catch (Exception ex)
            {
                dataDict.Set("ASAICashInFW - SOP - GetBvInfoLeft Error: " + ex.Message, "ASAIWELCOMETRACE");
                journal.Write(500002);
            }
            return 0;
        }

        private short GetBvInfosRight()
        {
            StringBuilder result = new StringBuilder();
            string resultFlag = string.Empty;

            try
            {
                WriteJournal("ASAICashInFW - SOP - GetBvInfosRight");

                MEI_BV_Devices.MeiEventArgs args = new MEI_BV_Devices.MeiEventArgs();

                args = api.meiCmdRight.GetStatus();

                resultFlag = "Port:                   " + args.configArgs.port + "\n";
                result.Append(resultFlag);
                resultFlag = "Position:               " + args.configArgs.position + "\n";
                result.Append(resultFlag);
                resultFlag = "Id:                     " + args.configArgs.id + "\n";
                result.Append(resultFlag);
                resultFlag = "Autoconnect:            " + args.configArgs.autoConnect + "\n";
                result.Append(resultFlag);
                resultFlag = "Autostack:              " + args.configArgs.autoStack + "\n";
                result.Append(resultFlag);
                resultFlag = "Barcodes enabled:       " + args.configArgs.enableBarcodes + "\n";
                result.Append(resultFlag);
                resultFlag = "PUP escrow action:      " + args.configArgs.pupEscrowAction + "\n";
                result.Append(resultFlag);
                resultFlag = "Escrow bill timeout:    " + args.configArgs.billReturnTimer + "\n";
                result.Append(resultFlag);
                resultFlag = "Escrow barcode timeout: " + args.configArgs.barcodeReturnTimer + "\n";
                result.Append(resultFlag);
                resultFlag = "Credit on stack:        " + args.configArgs.autoCreditOnStack + "\n";
                result.Append(resultFlag);

                result.Append("OK\n");

                CCSopDialogFW sop = new CCSopDialogFW();
                short aux = sop.DoDialog("ASAI_BB2_INFO_RESULT", result.ToString());

                WriteJournal("ASAICashInFW - SOP - GetBvInfosRight: " + result.ToString());
            }
            catch (Exception ex)
            {
                dataDict.Set("ASAICashInFW - SOP - GetBvInfosRight Error: " + ex.Message, "ASAIWELCOMETRACE");
                journal.Write(500002);
            }
            return 0;
        }

        #endregion SOP

        public void PostTransaction()
        {
            WriteJournal("PostTransaction: POSTING PARTIALLY:");
            string ddPosting = string.Empty;

            Transaction transaction;
            //TR Ticketing redemption code
            try
            {
                dataDict.Get(ref ddPosting, ASAICASHINFW_PROP_TRANSACTION);

                if (String.IsNullOrEmpty(ddPosting))
                {
                   WriteJournal(string.Format(">ASAILOFW. ASAICASHIN_TRANSACTION is null or Empty: '{0}'", ddPosting));                    
                }
                else
                {
                    transaction = DeserializeJSON(ddPosting);

                    Task t = Task.Run(() =>
                    {
                        PostData transactionTR = new PostData();
                        string sPosting = String.Empty;
                        transactionTR.type = ContractOperationTypes.OperationTypes_TicketRedemption;
                        transactionTR.accountType = ContractAccountTypes.AccountTypes_None;
                        transactionTR.seqNumber = transaction.seqNumber;

                        OperationInfo operationInfo = new OperationInfo();
                        operationInfo.SequenceNumber = transactionTR.seqNumber;
                        operationInfo.Amount = transaction.total;
                        operationInfo.BillDespensed = (Decimal)transaction.TotalDispensed();
                        operationInfo.CardNumber = "XXXX";

                        string sTotalCodes = "";
                        float ret = 0f;
                        if (transaction.items != null)
                        {
                            transaction.items.ForEach(tickets =>
                            {

                                string CIMNumber = "[" + tickets.billAcceptor + "]";
                                if (tickets.index > 1)
                                    sTotalCodes += ",";
                                sTotalCodes += tickets.code + CIMNumber + "     $" + tickets.value.ToString();

                            });
                        }

                        operationInfo.TicketData = sTotalCodes;
                        operationInfo.Status = ContractStatus.Status_PartialDispensing;
                        operationInfo.HostIP = "";
                        operationInfo.RegistrationDatetime = DateTime.Now;

                        TransactionDetailInfo transactionDetailInfo = new TransactionDetailInfo();
                        transactionDetailInfo.Amount = transaction.total;
                        transactionDetailInfo.ManualPay = 0m;
                        transactionDetailInfo.DispensedTotal = (Decimal)transaction.TotalDispensed();

                        transactionDetailInfo.BillDenom_01 = 1;
                        transactionDetailInfo.BillDenom_02 = 5;
                        transactionDetailInfo.BillDenom_03 = 10;
                        transactionDetailInfo.BillDenom_04 = 20;
                        transactionDetailInfo.BillDenom_05 = 50;
                        transactionDetailInfo.BillDenom_06 = 100;
                        transactionDetailInfo.CoinDenom_01 = 1;
                        transactionDetailInfo.CoinDenom_02 = 5;
                        transactionDetailInfo.CoinDenom_03 = 0;
                        transactionDetailInfo.CoinDenom_04 = 25;

                        transactionDetailInfo.BillCount_01 = 0;//Bill $1
                        transactionDetailInfo.BillCount_02 = 0;//Bill $5
                        transactionDetailInfo.BillCount_03 = 0;//Bill $10
                        transactionDetailInfo.BillCount_04 = 0;//Bill $20
                        transactionDetailInfo.BillCount_05 = 0;//Bill $50
                        transactionDetailInfo.BillCount_06 = 0;//Bill $100
                        transactionDetailInfo.CoinCount_01 = 0;//Hopper 0.01
                        transactionDetailInfo.CoinCount_02 = 0;//Hopper 0.05
                        transactionDetailInfo.CoinCount_04 = 0;//Hopper 0.25

                        #region Add Bill-Coins
                        if (transaction.bills != null)
                        {
                            transaction.bills.ForEach(bill =>
                            {

                                switch (bill.denomination)
                                {
                                    case 1:
                                        transactionDetailInfo.BillCount_01 += (Int16)(bill.numberOfNotes);
                                        break;
                                    case 5:
                                        transactionDetailInfo.BillCount_02 += (Int16)(bill.numberOfNotes);
                                        break;
                                    case 10:
                                        transactionDetailInfo.BillCount_03 += (Int16)(bill.numberOfNotes);
                                        break;
                                    case 20:
                                        transactionDetailInfo.BillCount_04 += (Int16)(bill.numberOfNotes);
                                        break;
                                    case 50:
                                        transactionDetailInfo.BillCount_05 += (Int16)(bill.numberOfNotes);
                                        break;
                                    case 100:
                                        transactionDetailInfo.BillCount_06 += (Int16)(bill.numberOfNotes);
                                        break;
                                    default:
                                        break;
                                }

                            });

                        }
                        if (transaction.coins != null)
                        {
                            transaction.coins.ForEach(coin =>
                            {

                                switch (coin.denomination)
                                {
                                    case 1:
                                        transactionDetailInfo.CoinCount_01 += (Int16)(coin.numberOfNotes);
                                        break;
                                    case 5:
                                        transactionDetailInfo.CoinCount_02 += (Int16)(coin.numberOfNotes);
                                        break;
                                    case 25:
                                        transactionDetailInfo.CoinCount_04 += (Int16)(coin.numberOfNotes);
                                        break;
                                    default:
                                        break;
                                }

                            });

                        }
                        #endregion Add Bill-Coins

                        TransactionAuthorizationInfo authorizationInfo = new TransactionAuthorizationInfo();
                        authorizationInfo.AuthNumber = "";
                        authorizationInfo.Fee = 0m;
                        authorizationInfo.RegisterTime = operationInfo.RegistrationDatetime;
                        authorizationInfo.OrderId = "";
                        authorizationInfo.TransactionNumber = long.Parse(transactionTR.seqNumber); // Optional
                        authorizationInfo.ErrorCode = "0";
                        authorizationInfo.ErrorDescription = "0";

                        transactionTR.Operation = operationInfo;
                        transactionTR.TransactionDetail = transactionDetailInfo;
                        transactionTR.Authorization = authorizationInfo;
                        sPosting = (Translator.SerializeJSON<PostData>(transactionTR));

                        dataDict.Set(sPosting, "ASAILOFW_POSTDATA");
                        string sPostData = "";
                        dataDict.Get(ref sPostData, "ASAILOFW_POSTDATA");

                        ASAILOFW postingLO = new ASAILOFW("");
                        postingLO.PostTransaction("");
                        WriteJournal("END PostTransaction: POSTING PARTIALLY:");
                    });
                }

            }
            catch (Exception ex)
            {
                WriteJournal($"PostTransaction Exception {ex.Message} InnerException {(ex.InnerException != null ? ex.InnerException.Message: "")}, StackTrace {ex.StackTrace}");
            }
        }

        #region OOS and InService

        private short OOS()
        {
            WriteJournal("ASAICashInFW - OOS");
         
            CCTopasApplicationFW fw = new CCTopasApplicationFW();
            short result = fw.OperatorSessionRequest();

            WriteJournal("ASAICashInFW - OOS OperatorSessionRequest: " + result);            

            while (result != 0)
            {
                System.Threading.Thread.Sleep(5000);
                result = fw.OperatorSessionRequest();
            }

            WriteJournal("ASAICashInFW - OOS OperatorSessionRequest END: " + result);            
            return result;
        }
        private void OOS_ThreadSafe()
        {
            WriteJournal("ASAICashInFW - OOS");

            CCTopasApplicationFW fw = new CCTopasApplicationFW();
            short result = fw.OperatorSessionRequest();

            WriteJournal("ASAICashInFW - OOS OperatorSessionRequest: " + result);

            while (result != 0)
            {
                System.Threading.Thread.Sleep(5000);
                result = fw.OperatorSessionRequest();
            }

            WriteJournal("ASAICashInFW - OOS OperatorSessionRequest END: " + result);
           
        }

        private short InService()
        {
            WriteJournal("ASAICashInFW - InService");            

            CCTopasApplicationFW fw = new CCTopasApplicationFW();
            short result = fw.OperatorSessionEndNotification();

            WriteJournal("ASAICashInFW - InService OperatorSessionEndNotification: " + result);

            return result;
        }
        private List<Tuple<string, int, bool>> GetIPandPort()
        {
            List <Tuple<string, int, bool>> list = new List<Tuple<string, int, bool>>();
            const string subkey = "Software\\WOW6432Node\\Wincor Nixdorf\\ProTopas\\CurrentVersion\\CCOPEN\\COMMUNICATION\\SSL\\CLIENT";
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(subkey))
            {
                if (key != null)
                {
                    var remotePeer = (string)key.GetValue("RemotePeer", "");
                    var remotePort = (string)key.GetValue("RemotePort", "0");
                    Int32.TryParse(remotePort, out int port);

                    if (!String.IsNullOrEmpty(remotePeer))
                    {
                        string hostIp;
                        if (!remotePeer.Contains(","))
                        {
                            hostIp = (!remotePeer.Contains("127.0.0.1") ? remotePeer : String.Empty);
                            list.Add(Tuple.Create(hostIp, port, true));
                        }
                        else
                        {
                            string[] ips = remotePeer.Split(',');
                            foreach (string ip in ips)
                            {
                                if (!ip.Contains("127.0.0.1"))
                                {
                                    list.Add(Tuple.Create(ip, port, true));
                                    WriteJournal($"ASAICashInFW - GetIPandPort/ Ip {ip} Port {port}");
                                }
                            }
                        }
                        return list;

                    }
                    else
                    {
                        WriteJournal($"ASAICashInFW - GetIPandPort/RemotePeer is empty");
                        list.Add(Tuple.Create("", 0, false));
                        return list;
                    }


                }
                else
                {
                    WriteJournal($"ASAICashInFW - GetIPandPort/OpenSubkey {subkey} is empty");
                    list.Add(Tuple.Create("", 0, false));
                    return list;
                }

            }
        }
        private bool PrimaryHostIsConnected()
        {
         
            WriteJournal($"ASAICashInFW - PrimaryHostIsConnected starting");
            bool result = false;
            var values = GetIPandPort();
            foreach (var value in values){
                string hostIp = value.Item1;
                int port = value.Item2;
                bool validation = value.Item3;
                if (validation)
                {
                    TcpClient hostClient = new TcpClient();
                    try
                    {
                        result = hostClient.ConnectAsync(hostIp, port).Wait(TimeSpan.FromSeconds(5));
                        WriteJournal($"ASAICashInFW - PrimaryHostIsConnected- {hostIp}:{port} result {result}");
                        if (result)
                        {
                            hostClient.Close();
                            return result;
                        }
                    }
                    catch (Exception e)
                    {
                        WriteJournal($"ASAICashInFW - PrimaryHostIsConnected- Exception {e.ToString()}");
                        result = false;
                    }
                }
                
                
            }
            
            WriteJournal($"ASAICashInFW - PrimaryHostIsConnected result {result}");

            return result;
        }

        private void ReadMinimizeShortPay()
        {
            //Read the MinimizeShortPays parameters  
            try
            {
                Task t = Task.Run(() =>
                {

                    string kioskParameters = String.Empty;
                    string minimizeShortPaystr = String.Empty;
                    liveOffice.GetKioskParameters("MinimizeShortPays");
                    dataDict.Get(ref kioskParameters, "ASAILOFW_KIOSKPARAMETERS");
                    JObject kioskParam = JObject.Parse(kioskParameters);
                    minimizeShortPaystr = kioskParam["Result"]["MinimizeShortPays"].ToString();
                    WriteJournal("ASAICharityFW - ReadMinimizeShortPay minimizeShortPay: " + minimizeShortPaystr);
                    minimizeShortPay = (minimizeShortPaystr.ToUpper() == "TRUE") ? true : false;
                });
            }
            catch(Exception ex)
            {
                WriteJournal("ASAICashInFW - MinimizeShortPay Error: " + ex.Message);
                minimizeShortPay = false;
            }
            //dataDict.Set(minimizeShortPaystr, "MINIMIZESHORTPAYS");
        }

        #endregion



    }
}
