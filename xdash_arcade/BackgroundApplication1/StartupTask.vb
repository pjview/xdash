Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Net.Http
Imports Windows.ApplicationModel.Background
Imports Windows.Gaming.Input
Imports Windows.System
Imports Windows.Devices.Power
Imports System.Diagnostics
Imports System.ComponentModel

' The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

' --- SimTool s : Interface Setting
' Port 15577
' Output    - Bit Range [16] Type [Decimal]
' Startup   - Output "[0]sys.write(1101,1):[0]sys.write(100,2)" 3000 ms
' Interface - Output "[0]sys.pos(<Axis1a>,<Axis2a>,<Axis3a>,<Axis4a>,<Axis5a>,<Axis6a>)" 25 ms
' Shutdown  - Output "[0]sys.write(100,1)" 1000 ms

Enum TelemitryCMD_ID
    'high piority
    ACTUAL_POSITION_M0 = 0
    MOTOR_CURRENT_M0 = 1
    ACTUAL_POSITION_M1 = 2
    MOTOR_CURRENT_M1 = 3
    'low piority
    ERROR_M0 = 4
    ERROR_M1 = 5
    TEMPERATURE_M0 = 6
    TEMPERATURE_M1 = 7
End Enum

Public NotInheritable Class StartupTask

    Implements IBackgroundTask
    Implements IGameController, IGameControllerBatteryInfo

    Dim BGTask As BackgroundTaskDeferral = Nothing
    Dim TaskCancle As Boolean = False
    Dim NETTransmitter As System.Net.Sockets.UdpClient
    'Dim NETEventTransmitter As System.Net.Sockets.UdpClient
    'Dim NETDBTransmitter As System.Net.Sockets.UdpClient
    'Dim NETRecever As System.Net.Sockets.UdpClient

    'Dim NETTelemetry As System.Net.Sockets.UdpClient
    Private Const ErrorValue_Int As Integer = -32768
    Private Const ErrorValue_Single As Single = -32768.0

    Dim performanceTimer_start As Long = 0
    Dim performanceTimer_end As Long = 0

    Dim ZCon As RawGameController
    Dim ZWheel As RacingWheel
    Dim ZPad As Gamepad
    Dim ZArcade As ArcadeStick
    Dim ZFlight As FlightStick
    Dim GameControllerFound As Boolean = False
    Public Shared ReadOnly Property RawGameControllers As IReadOnlyList(Of RawGameController)
    Public Shared ReadOnly Property RacingWheels As IReadOnlyList(Of RacingWheel)
    Public Shared ReadOnly Property Gamepads As IReadOnlyList(Of Gamepad)
    Public Shared ReadOnly Property ArcadeSticks As IReadOnlyList(Of ArcadeStick)
    Public Shared ReadOnly Property FlightSticks As IReadOnlyList(Of FlightStick)

    'Const DefaultIP As String = "192.168.148.160" 'default respond ip address
    'Const DashboardTXPort As Integer = 15566
    'Const CommandRXPort As Integer = 16677 '<-- 0 = All Port (original config tool use 15577)
    'Const CommandTXPort As Integer = 15588
    'Const TelemetryTXPort As Integer = 15599

    Dim NetBinding As CustomIPBinding
    Dim EndpoinAddress As Net.IPAddress
    Dim LocalAddress As Net.IPAddress
    Dim PCEndpoint As System.Net.IPEndPoint
    'Dim ControlEndpoint As System.Net.EndPoint
    Dim DashboardEndpoint As System.Net.EndPoint
    Dim LogAddress As System.Net.IPAddress
    Dim TelemetryAddress As System.Net.IPAddress
    Dim DashBoardAddress As System.Net.IPAddress
    'Dim TelemetryEndpoint As System.Net.IPEndPoint
    Dim EndpointINIT As Integer = 0
    Dim CommandReturnPort As Integer = 15588 'default 15588
    Dim RXBuffer(256) As Byte
    Dim TXBuffer(512) As Byte

    Dim DeltaTime As UInt64 = 0

    Dim Wheel As Reg.CustomController
    Dim Wheel_Shadow As Reg.CustomController
    Dim DriveError(10) As Reg.CustomDriveError

    Dim I2CDevice As Windows.Devices.I2c.I2cDevice
    Dim I2CSetup As Windows.Devices.I2c.I2cConnectionSettings
    Dim I2COK As Boolean = False
    Dim GPIO As Windows.Devices.Gpio.GpioController
    Dim GPIOPIN(32) As Windows.Devices.Gpio.GpioPin
    Const TotalUART As Integer = 7 'max number of uart device support
    Dim UARTDevice(TotalUART) As Windows.Devices.SerialCommunication.SerialDevice
    Dim UARTNameID(TotalUART) As String
    Dim CommWriter(TotalUART) As Windows.Storage.Streams.DataWriter
    Dim CommReader(TotalUART) As Windows.Storage.Streams.DataReader
    Dim CommRX(TotalUART) As String
    Dim CommRXShadow(TotalUART) As String
    Dim WithEvents CommandReceverTask As New System.ComponentModel.BackgroundWorker
    Dim WithEvents BroadcastReceverTask As New System.ComponentModel.BackgroundWorker
    Dim WithEvents WheelReceverTask As New System.ComponentModel.BackgroundWorker

    Dim Ascii As System.Text.Encoding = System.Text.Encoding.ASCII
    Dim OpenGPIOPin(255) As Boolean
    '---------------------------------0--1--2--3---4---5---6---7---8---9---10--11--12
    Dim GPIOAvailablePin() As Byte = {4, 5, 6, 12, 13, 17, 18, 22, 23, 24, 25, 26, 27}
    Dim GPIOInputStatus(255) As Byte
    Dim UseComm(32) As Boolean
    Dim DirPin(100) As Byte
    Dim I2CDataShort(1) As Byte
    Dim I2CMap(255) As Byte
    '-----------------------0------1--------2-------3-------4--------5---------6---------7----------8--------9----------10-------11-------12--------
    Dim Dict() As String = {"SYS", "GPIO(", "I2C(", "SPI(", "UART(", "DRIVE(", ".READ(", ".WRITE(", ".USE(", ".DEVICE", ".LIST", ".POS(", ".STOP"}
    Dim QDict(20) As String
    Dim CMDRes As New StringBuilder
    Dim CMDDataOut As New StringBuilder
    Dim CMDDataDrv0 As New StringBuilder
    Dim CMDDataDrv1 As New StringBuilder
    Dim CMDDataDrv2 As New StringBuilder
    Dim CMDAvailableDrv0 As Boolean = False
    Dim CMDAvailableDrv1 As Boolean = False
    Dim CMDAvailableDrv2 As Boolean = False
    Dim RecvCMDID As Integer = 0
    Dim CMDBUSY As Boolean = False
    Const ASCIIlineFeed As String = ChrW(10)
    Const LCDEmptyLine As String = "                "
    Dim CommandID As Integer = -1


    Dim Telemetry_Slot_Select As Integer = 0
    Dim Telemetry_DRV_Select As Integer = -1
    Dim Telemetry_AXIS_Select As Integer = 0
    Dim Telemetry_Request_ID(10) As Integer

    Const Telemetry_POS_Target As Integer = 0
    Const Telemetry_POS_RPM_Actual As Integer = 1
    Const Telemetry_CUR_Command As Integer = 3
    Const Telemetry_CUR_Measured As Integer = 4

    Const Motor_ON As Integer = 1
    Const Motor_OFF As Integer = 0
    Const Profile_FAST As Integer = 1
    Const Profile_SLOW As Integer = 0

    Dim StreamWait(5) As Boolean
    'Dim StreamLock(5) As Boolean
    'Dim streamCMD As String = "f "
    Dim TempString(10) As String
    Dim TempNumber(10) As Double
    'Dim TelemetryDataS(4, 10) As String
    'Dim TelemetryDataV(4, 10) As Double
    Dim TelemetryIndex(4) As Integer
    Dim TelemetryCounter(4) As Integer
    Dim TelemetryIdleCount(4) As Integer
    'Dim COMMBusy(99) As Boolean

    Dim TelemetryEnable As Boolean = False

    Dim netStorage As Windows.Storage.StorageFolder '= Windows.Storage.StorageFolder.GetFolderFromPathAsync("\\192.168.148.200\DashLog")
    Dim NextionWDT As Integer = 0
    Dim DisplayOK As Boolean = False
    Dim FTPOK As Boolean = False
    Dim MotorProfile_Shadow As Integer = 99
    Dim NextionPage_Shadow As Integer = 99
    Dim NextionUpdate_Inprogress As Boolean = False
    Dim StartupSequence_Finish As Boolean = False
    Dim SEQIDShadow As Integer = 0

    Const RingBufferSize As Integer = 50
    Dim RingBuffer(6, RingBufferSize) As String
    Dim RingWriteIndex(6) As Integer
    Dim RingReadIndex(6) As Integer
    'Dim RingPacketSize As Integer = 64
    Dim RingByteBuffer(6, RingBufferSize)() As Byte

    Const RespondBufferSize As Integer = 50
    Const RespondPacketSizeIndicator As Integer = 1
    Const RespondPacketSize As Integer = 256
    Const RespondByteMaxSize As Integer = 2 * RespondPacketSize * (RespondBufferSize + 1)
    'Dim RespondBuffer(2, RespondBufferSize) As String
    Dim RespondByteBuffer(RespondByteMaxSize) As Byte
    Dim RespondByteWritePointer As Integer
    Dim RespondByteReadPointer As Integer
    Dim RespondWriteIndex(2) As Integer
    Dim RespondReadIndex(2) As Integer

    Dim WriteLock As New Object
    Dim WriteByteLock As New Object
    Dim RespondLock As New Object

    '--- Analog Input Offset --- (Range 0-1023)
    Const Loadcell_Offset As Integer = 629
    Const Loadcell_Scale As Single = 0.325203252
    Const Temperature0_Offset As Integer = 0
    Const Temperature1_Offset As Integer = 0
    Const Temperature2_Offset As Integer = 0
    Const Temperature_Offset As Integer = 0

    '--- Known Device ---
    Dim Drive0_DeviceName As String = ""
    Dim Drive1_DeviceName As String = ""
    Dim Drive2_DeviceName As String = ""
    Dim Drive3_DeviceName As String = ""
    'Dim Sensor0_DeviceName As String = "\\?\USB#VID_2341&PID_0043#5573032373135141D070#{86e0d1e0-8089-11d0-9ce4-08003e301f73}"
    Dim Display_DeviceName As String = ""
    Dim Control_DeviceName As String = ""
    Dim Local_DeviceName As String = ""
    Dim System_Name As String = ""
    Dim Drive0_Baud As Integer = -1
    Dim Drive1_Baud As Integer = -1
    Dim Drive2_Baud As Integer = -1
    Dim Drive3_Baud As Integer = -1
    Dim Display_Baud As Integer = -1
    Dim Control0_Baud As Integer = -1
    Dim Local_Baud As Integer = -1
    'Dim ODriveWheel_ID As Integer = 1
    Dim Control_OK As Boolean = False

    '--- Register Table ---
    Dim IORegister(4, 256) As Single
    Dim ControlRegister(200) As Single
    Dim StateRegister(200) As Integer
    Dim ControlString(10) As String
    Dim TotalRevolution(6) As Long
    Dim PositionShadow(6) As Integer
    Dim MotorStallOffset(8) As Single
    Dim TotalTime(6) As Long
    Dim ConfigOK(10) As Boolean
    Dim EmergencySTOP As Boolean = False
    Dim Machine_Stat(10) As Integer

    '------ default startup mechanical parameter ------
    '--- !!! DANGER !!! MISCONFIG CAN DAMAGE SYSTEM ---
    'Const Axis0_HomeDeg As Single = 67.85        '<-- drive:0 axis:0 gear box output shaft home position (Degree) (read X register)
    'Const Axis0_HomeDir As Single = 1.0         '<-- home direction

    'Const Axis1_HomeDeg As Single = 55.55    '<-- drive:0 axis:1 gear box output shaft home position (Degree) (read Y register)
    'Const Axis1_HomeDir As Single = -1.0        '<-- home direction

    'Const Axis2_HomeDeg As Single = 316.05      '<-- drive:1 axis:0 gear box output shaft home position (Degree) (read Z register)
    'Const Axis2_HomeDir As Single = 1.0         '<-- home direction

    'Const Axis3_HomeDeg As Single = 0.0         '<-- drive:1 axis:1 gear box output shaft home position (Degree) (read A register)
    'Const Axis3_HomeDir As Single = 1.0         '<-- home direction

    'Const Axis4_HomeDeg As Single = 0.0
    'Const Axis4_HomeDir As Single = 0.0

    'Const AxisDiff_Limit As Single = 1.5        '<-- maximum allow axis diff (Degree)
    'Const AxisDiff_Timeout As Integer = 3000    '<-- axis diff correction timeout (mSec)
    'Const PPRTransis_Limit As Double = 170000.0 '<-- !!! Maximum value is 196608.0 (in PPR not RPM unit)
    'Const Home_Limit As Single = 30.0           '<-- Maximum home move in degree

    'Const Axis0_PPR As Double = 2048.0 '<-- encoder state per revolution (pulse per revolution x 4)
    'Const Axis1_PPR As Double = 2048.0
    'Const Axis2_PPR As Double = 2048.0
    'Const Axis3_PPR As Double = 2048.0
    'Const Axis4_PPR As Double = 2048.0
    'Const Axis5_PPR As Double = 2048.0

    'Const Axis0_RotationRange As Double = 40.0 '<-- gear box output shaft limit (+- degree)
    'Const Axis1_RotationRange As Double = 40.0
    'Const Axis2_RotationRange As Double = 40.0
    'Const Axis3_RotationRange As Double = 40.0
    'Const Axis4_RotationRange As Double = 40.0
    'Const Axis5_RotationRange As Double = 40.0

    'Const Gear0_Raito As Double = 80.0 '<-- gear box raito
    'Const Gear1_Raito As Double = 80.0
    'Const Gear2_Raito As Double = 80.0
    'Const Gear3_Raito As Double = 1.0
    'Const Gear4_Raito As Double = 1.0
    'Const Gear5_Raito As Double = 1.0

    'Const Axis0_PPD As Double = ((Axis0_PPR * Gear0_Raito) / 360.0) '<--- pulse per degree
    'Const Axis1_PPD As Double = ((Axis1_PPR * Gear1_Raito) / 360.0)
    'Const Axis2_PPD As Double = ((Axis2_PPR * Gear2_Raito) / 360.0)
    'Const Axis3_PPD As Double = ((Axis3_PPR * Gear3_Raito) / 360.0)
    'Const Axis4_PPD As Double = ((Axis4_PPR * Gear3_Raito) / 360.0)
    'Const Axis5_PPD As Double = ((Axis5_PPR * Gear3_Raito) / 360.0)

    'Const Shaft_Speed As Double = 62.2 '<-- shaft speed limit (in RPM)

    'Const Axis0_Output_SpeedLimit As Double = 170000
    'Const Axis1_Output_SpeedLimit As Double = 170000
    'Const Axis2_Output_SpeedLimit As Double = 170000
    'Const Axis3_Output_SpeedLimit As Double = 170000 'this formula is    Shaft_Speed=((X/PPR)*60)/Gear_Raito

    'Const Axis0_HomeRange As Double = Axis0_PPD * Home_Limit
    'Const Axis1_HomeRange As Double = Axis1_PPD * Home_Limit
    'Const Axis2_HomeRange As Double = Axis2_PPD * Home_Limit
    'Const Axis3_HomeRange As Double = 1.0 '((Axis3_PPR * Gear3_Raito) / 360.0) * Home_Limit
    'Const Axis4_HomeRange As Double = 1.0
    'Const Axis5_HomeRange As Double = 1.0

    'Const Input_Range As Double = 2 ^ 16 'simtool 16bit output range
    'Const Input_Offset As Double = Input_Range / 2 'for simtool only

    'Const Axis0_PRange As Double = Axis0_PPD * (Axis0_RotationRange * 1)
    'Const Axis1_PRange As Double = Axis1_PPD * (Axis1_RotationRange * 1)
    'Const Axis2_PRange As Double = Axis2_PPD * (Axis2_RotationRange * 1)
    'Const Axis3_PRange As Double = 7000 '((Axis3_PPR * Gear3_Raito) / 360.0) * (Axis3_RotationRange * 2)
    'Const Axis4_PRange As Double = Axis4_PPD * (Axis4_RotationRange * 1)
    'Const Axis5_PRange As Double = Axis5_PPD * (Axis5_RotationRange * 1)

    'Const Axis0_MRange As Double = Axis0_PRange * -1
    'Const Axis1_MRange As Double = Axis1_PRange * -1
    'Const Axis2_MRange As Double = Axis2_PRange * -1
    'Const Axis3_MRange As Double = Axis3_PRange * -1
    'Const Axis4_MRange As Double = Axis4_PRange * -1
    'Const Axis5_MRange As Double = Axis5_PRange * -1

    'Const Axis0_Scale As Double = (Axis0_PRange / 100.0) / 2 '/ Input_Range
    'Const Axis1_Scale As Double = (Axis1_PRange / 100.0) / 2 '/ Input_Range
    'Const Axis2_Scale As Double = (Axis2_PRange / 100.0) / 2 '/ Input_Range
    'Const Axis3_Scale As Double = (Axis3_PRange / 100.0) / 1 '/ Input_Range
    'Const Axis4_Scale As Double = (Axis4_PRange / 100.0) / 1 '/ Input_Range
    'Const Axis5_Scale As Double = (Axis5_PRange / 100.0) / 1 '/ Input_Range

    Dim Axis_Scale(10) As Double
    'Dim Axis_PPD(10) As Double

    'Const Axis0_Inv As Boolean = False
    'Const Axis1_Inv As Boolean = False
    'Const Home0_Offset As Integer = 0 '<-- Must use PPR scale --
    'Const Home1_Offset As Integer = 0 '<-- Must use PPR scale --
    'Const Home0_Speed As Integer = 500 'RPM
    'Const Home1_Speed As Integer = 500 'RPM
    'Const DistLimit_Range As Integer = 4551 'this formula is   DistLimit_Range = ((PPR * Gear_Raito) / 360.0) * Range_InDegree)
    Const StatusLED As Integer = 3 'Status LED GPIO pin (please refer to GPIOAvailablePin array)

    Const SuddenMove_Limit As Single = 0.25 ' limit movement between command (turn/command)

    '[Reg.Axis1_Offset] <-- Must use PPR scale --
    '[Reg.Axis0_Offset] <-- Must use PPR scale --
    Dim Script_RUN As Boolean = False

    Dim Axis_Temp As Single = 0.0
    Dim Output_Axis As AxisMath.AxisStruct

    '--- Parameter & Config ---
    Dim NetConfig As Storage.Network_Config
    Dim OPConfig As Storage.OP_Config

    '--- Debug mode config ----
    Const VerbrossEnable As Boolean = True
    Const DebugEnable As Boolean = False
    'Const ScriptEnable As Boolean = True
    Const AutoHome As Boolean = True

    '=== !!! === DO NOT ENABLE UNTIL BOARD FIXED === !!! ===
    '
    'can made sensor PSU overvoltage if control software not start or exit properly
    Const AVRSoftwareControl As Boolean = False
    '
    'working but can stay off if not reset and cause false sense of unconnect power source
    Const ProtectionRelayOverride As Boolean = False
    '
    'this not hardware relate ...
    'lead to unstable system timer run as non consistant speed (timing error -> comm lost)
    Const HighPioritySchdue As UInt32 = 31 '(31 = defaule , 8 = real time)
    '
    '
    '=======================================================

    Public Sub Run(taskInstance As IBackgroundTaskInstance) Implements IBackgroundTask.Run

        'Autostart script variable Init()
        Dim Drive_OK(4) As Boolean
        Dim SensorOK As Boolean = False
        Dim DOFCount As Integer = 0
        Dim MOTORCount As Integer = 0

        Dim Enc_OK(6) As Boolean
        Dim StartUpERROR As Boolean = False
        Dim Axis0_CurrentPos As Single = 0.0
        Dim Axis1_CurrentPos As Single = 0.0
        Dim Gear0_CurrentDeg As Single = 0.0
        Dim Gear1_CurrentDeg As Single = 0.0
        Dim Axis0_RequestMove As Single = 0.0
        Dim Axis1_RequestMove As Single = 0.0
        Dim Axis0_Diff As Single = 0.0
        Dim Axis1_Diff As Single = 0.0


        'Handler for cancelled background task
        AddHandler taskInstance.Canceled, New BackgroundTaskCanceledEventHandler(AddressOf onCanceled)

        'Flag + Register init
        'System.Diagnostics.process
        'System.Array.Clear(TelemetryMode, 0, TelemetryMode.Length)
        System.Array.Clear(MotorStallOffset, 0, MotorStallOffset.Length)
        System.Array.Clear(ControlRegister, 0, ControlRegister.Length)
        System.Array.Clear(RXBuffer, 0, RXBuffer.Length)
        System.Array.Clear(TXBuffer, 0, TXBuffer.Length)
        'System.Array.Clear(COMMBusy, 0, COMMBusy.Length)
        System.Array.Clear(RingBuffer, 0, RingBuffer.Length)
        System.Array.Clear(RingByteBuffer, 0, RingByteBuffer.Length)
        'For i As Integer = 0 To RingBufferSize
        '    ReDim RingByteBuffer(i)(RingPacketSize)
        'Next
        For i As Integer = 0 To DriveError.Length - 1
            DriveError(i).General = 0
            DriveError(i).Motor = 0
            DriveError(i).Encoder = 0
        Next

        ControlRegister(Reg.ColdStart) = 1
        'ControlRegister(Reg.ScriptRequest) = 1
        ControlRegister(Reg.Game_Controller_Type) = -1 'disable game controller until device discovery finished
        'ControlRegister(Reg.Axis0_GearCenter) = Axis0_HomeDeg
        'ControlRegister(Reg.Axis1_GearCenter) = Axis1_HomeDeg

        'AxisMath.DashDash_Init()
        'AxisMath.DashDash_2Axis_Init()
        'AxisMath.DashDash_4Axis_Init()
        Log.Init()
        AxisMath.Default_Init()
        AxisMath.setUsage(100)
        AxisMath.useSimtools(True) '--- use simtool input compatible mode

        Dim Statistics As Storage.Machine_Statistics = Storage.loadRecord
        ControlRegister(Register.Axis0_TotalRev) = Statistics.Axis0_Revolution
        ControlRegister(Register.Axis1_TotalRev) = Statistics.Axis1_Revolution
        ControlRegister(Register.Axis2_TotalRev) = Statistics.Axis2_Revolution
        ControlRegister(Register.Axis3_TotalRev) = Statistics.Axis3_Revolution
        ControlRegister(Register.Axis0_TotalTime) = Statistics.Axis0_Time
        ControlRegister(Register.Axis1_TotalTime) = Statistics.Axis1_Time
        ControlRegister(Register.Axis2_TotalTime) = Statistics.Axis2_Time
        ControlRegister(Register.Axis3_TotalTime) = Statistics.Axis3_Time


        'For i As Integer = 0 To 3 'fill telemetry string buffer
        'For j As Integer = 0 To 9
        'TelemetryDataS(i, j) = ""
        'Next
        'Next


        Script_RUN = True

        'Debug.WriteLine("SD-Card Space : " & Storage.getSpace("C:"))
        'Debug.WriteLine("User Storeage Size : ")

        '---- Network init ----
        Dim LocalIP As System.Net.IPAddress = System.Net.IPAddress.Parse("0.0.0.0")
        NetBinding.LocalIP = LocalIP
        For Each host As Windows.Networking.HostName In Windows.Networking.Connectivity.NetworkInformation.GetHostNames
            If System.Net.IPAddress.TryParse(host.DisplayName, LocalIP) Then
                If LocalIP.AddressFamily.Equals(System.Net.Sockets.AddressFamily.InterNetwork) Then NetBinding.LocalIP = LocalIP
            End If
        Next
        SysEvent("Local IP : " & NetBinding.LocalIP.ToString)

        NetConfig = Storage.LoadNetConfig
        Array.Clear(RXBuffer, 0, RXBuffer.Length)
        Array.Clear(TXBuffer, 0, TXBuffer.Length)
        LogAddress = NetConfig.FTPServer_IP
        TelemetryAddress = NetConfig.Telemetry_IP

        If NetConfig.Control_IP Is Nothing Then NetConfig.Control_IP = System.Net.IPAddress.Parse("127.0.0.1") ' --- 127.0.0.1
        If NetConfig.Dashboard_IP Is Nothing Then NetConfig.Dashboard_IP = LocalIP

        Debug.WriteLine("HostControl_IP " & NetConfig.Control_IP.ToString)
        Debug.WriteLine("Control_PortRX " & NetConfig.Control_PortRX.ToString)
        Debug.WriteLine("Control_PortTXSource " & NetConfig.Control_PortTXSource.ToString)
        Debug.WriteLine("Control_PortTXTarget " & NetConfig.Control_PortTXTarget.ToString)

        Debug.WriteLine("Dashboard_IP " & NetConfig.Dashboard_IP.ToString)
        Debug.WriteLine("Dashboard_PortTXSource " & NetConfig.Dashboard_PortTXSource.ToString)
        Debug.WriteLine("DashboardStatus_PortTXTarget " & NetConfig.DashboardStatus_PortTXTarget.ToString)
        Debug.WriteLine("DashboardCommand_PortTXTarget " & NetConfig.DashboardCommand_PortTXTarget.ToString)

        Debug.WriteLine("Telemetry_IP " & NetConfig.Telemetry_IP.ToString)
        Debug.WriteLine("Telemetry_PortTXSource " & NetConfig.Telemetry_PortTXSource.ToString)
        Debug.WriteLine("Telemetry_PortTXTarget " & NetConfig.Telemetry_PortTXTarget.ToString)

        'NETRecever = New System.Net.Sockets.UdpClient(NetConfig.Control_PortRX)
        NetBinding.LocalIP = LocalIP
        NetBinding.commandRXPort = NetConfig.Control_PortRX
        NetBinding.broadcastRXPort = 22200
        NetBinding.IPBinding_A = NetConfig.Control_IP
        NetBinding.IPBinding_B = NetConfig.Dashboard_IP
        NETTransmitter = New System.Net.Sockets.UdpClient(NetConfig.Control_PortTXSource)
        'NETEventTransmitter = New System.Net.Sockets.UdpClient  
        'NETDBTransmitter = New System.Net.Sockets.UdpClient()
        PCEndpoint = New System.Net.IPEndPoint(NetConfig.Control_IP, NetConfig.Control_PortTXTarget)
        DashboardEndpoint = New System.Net.IPEndPoint(NetConfig.Dashboard_IP, NetConfig.DashboardCommand_PortTXTarget)
        Log.Init_Dashboard(NetConfig.Dashboard_IP, NetConfig.Dashboard_PortTXSource, NetConfig.DashboardStatus_PortTXTarget)
        Log.Init_Live(NetConfig.Telemetry_IP, NetConfig.Telemetry_PortTXSource, NetConfig.Telemetry_PortTXTarget)
        For Each XIP As Net.IPAddress In Net.Dns.GetHostAddressesAsync(Net.Dns.GetHostName).Result
            If XIP.AddressFamily = Net.Sockets.AddressFamily.InterNetwork Then
                LocalAddress = XIP
            End If
        Next

        '---- init motor status (dashboard text) ----
        For i As Integer = 0 To Log.Data.Axis_StateTXT.Length - 1
            Log.Data.Axis_StateTXT(i) = "not initialize"
        Next

        '---- Hardware ( device inint ) ----
        Dim HWConfError As Boolean = False
        Dim HWConfig As Storage.Hardware_Config = Storage.LoadHWConfig
        Drive0_DeviceName = HWConfig.Drive0_DeviceName
        Drive1_DeviceName = HWConfig.Drive1_DeviceName
        Drive2_DeviceName = HWConfig.Drive2_DeviceName
        Drive3_DeviceName = HWConfig.Drive3_DeviceName
        Display_DeviceName = HWConfig.Display_DeviceName
        Control_DeviceName = HWConfig.Control_DeviceName
        Local_DeviceName = LoadHWConfig.Local_DeviceName
        System_Name = LoadHWConfig.System_Name
        Drive0_Baud = HWConfig.Drive0_BPS
        Drive1_Baud = HWConfig.Drive1_BPS
        Drive2_Baud = HWConfig.Drive2_BPS
        Drive3_Baud = HWConfig.Drive3_BPS
        Display_Baud = HWConfig.Display_BPS
        Control0_Baud = HWConfig.Control_BPS
        Local_Baud = HWConfig.Local_BPS
        If HWConfig.Drive0_DeviceName <> "" And HWConfig.Drive0_BPS < 0 Then HWConfError = True : SysEvent("Drive-0 Invalid config DeviceName[" & HWConfig.Drive0_DeviceName & "] BPS:" & HWConfig.Drive0_BPS.ToString)
        If HWConfig.Drive1_DeviceName <> "" And HWConfig.Drive1_BPS < 0 Then HWConfError = True : SysEvent("Drive-1 Invalid config DeviceName[" & HWConfig.Drive1_DeviceName & "] BPS:" & HWConfig.Drive1_BPS.ToString)
        If HWConfig.Drive2_DeviceName <> "" And HWConfig.Drive2_BPS < 0 Then HWConfError = True : SysEvent("Drive-2 Invalid config DeviceName[" & HWConfig.Drive2_DeviceName & "] BPS:" & HWConfig.Drive2_BPS.ToString)
        If HWConfig.Drive3_DeviceName <> "" And HWConfig.Drive3_BPS < 0 Then HWConfError = True : SysEvent("Drive-3 Invalid config DeviceName[" & HWConfig.Drive3_DeviceName & "] BPS:" & HWConfig.Drive3_BPS.ToString)
        If HWConfig.Control_DeviceName <> "" And HWConfig.Control_BPS < 0 Then HWConfError = True : SysEvent("Control Invalid config DeviceName[" & HWConfig.Control_DeviceName & "] BPS:" & HWConfig.Control_BPS.ToString)
        If HWConfig.Display_DeviceName <> "" And HWConfig.Display_BPS < 0 Then HWConfError = True : SysEvent("Display Invalid config DeviceName[" & HWConfig.Display_DeviceName & "] BPS:" & HWConfig.Display_BPS.ToString)
        If HWConfig.Local_DeviceName <> "" And HWConfig.Local_BPS < 0 Then HWConfError = True
        'Debug.WriteLine("Dev0 BAUD:" & Drive0_Baud.ToString)
        'Debug.WriteLine("Dev1 BAUD:" & Drive1_Baud.ToString)
        'Debug.WriteLine("Dev2 BAUD:" & Drive2_Baud.ToString)
        'Debug.WriteLine("Dev3 BAUD:" & Drive3_Baud.ToString)
        'Debug.WriteLine("Cont BAUD:" & Control0_Baud.ToString)
        'Debug.WriteLine("Disp BAUD:" & Display_Baud.ToString)
        'Debug.WriteLine("SUBS BAUD:" & Local_Baud.ToString)
        System.Array.Clear(UseComm, 0, UseComm.Length)
        If HWConfError Then SysEvent("Hardware Config Error !")

        '---- Hardware ( Motor Mapping ) ----
        ODrive.MotorMapping(0, HWConfig.Motor0_DriveNo, HWConfig.Motor0_OutputNo, HWConfig.Motor0_SensorNo)
        ODrive.MotorMapping(1, HWConfig.Motor1_DriveNo, HWConfig.Motor1_OutputNo, HWConfig.Motor1_SensorNo)
        ODrive.MotorMapping(2, HWConfig.Motor2_DriveNo, HWConfig.Motor2_OutputNo, HWConfig.Motor2_SensorNo)
        ODrive.MotorMapping(3, HWConfig.Motor3_DriveNo, HWConfig.Motor3_OutputNo, HWConfig.Motor3_SensorNo)
        ODrive.MotorMapping(4, HWConfig.Motor4_DriveNo, HWConfig.Motor4_OutputNo, HWConfig.Motor4_SensorNo)
        ODrive.MotorMapping(5, HWConfig.Motor5_DriveNo, HWConfig.Motor5_OutputNo, HWConfig.Motor5_SensorNo)
        ODrive.MotorMapping(6, HWConfig.Motor6_DriveNo, HWConfig.Motor6_OutputNo, HWConfig.Motor6_SensorNo)
        ODrive.MotorMapping(7, HWConfig.Motor7_DriveNo, HWConfig.Motor7_OutputNo, HWConfig.Motor7_SensorNo)

        Data.Axis_DriverNo(0) = HWConfig.Motor0_DriveNo
        Data.Axis_DriverNo(1) = HWConfig.Motor1_DriveNo
        Data.Axis_DriverNo(2) = HWConfig.Motor2_DriveNo
        Data.Axis_DriverNo(3) = HWConfig.Motor3_DriveNo
        Data.Axis_DriverNo(4) = HWConfig.Motor4_DriveNo
        Data.Axis_DriverNo(5) = HWConfig.Motor5_DriveNo
        Data.Axis_DriverNo(6) = HWConfig.Motor6_DriveNo
        Data.Axis_DriverNo(7) = HWConfig.Motor7_DriveNo
        MOTORCount = 0
        For i As Integer = 0 To 7
            If Data.Axis_DriverNo(i) >= 0 Then MOTORCount += 1
        Next
        NetBinding.AUXData_MotorCount = MOTORCount

        Debug.WriteLine("Motor0 Drv:" & HWConfig.Motor0_DriveNo.ToString & " CH:" & HWConfig.Motor0_OutputNo.ToString)
        Debug.WriteLine("Motor1 Drv:" & HWConfig.Motor1_DriveNo.ToString & " CH:" & HWConfig.Motor1_OutputNo.ToString)
        Debug.WriteLine("Motor2 Drv:" & HWConfig.Motor2_DriveNo.ToString & " CH:" & HWConfig.Motor2_OutputNo.ToString)
        Debug.WriteLine("Motor3 Drv:" & HWConfig.Motor3_DriveNo.ToString & " CH:" & HWConfig.Motor3_OutputNo.ToString)
        Debug.WriteLine("Motor4 Drv:" & HWConfig.Motor4_DriveNo.ToString & " CH:" & HWConfig.Motor4_OutputNo.ToString)
        Debug.WriteLine("Motor5 Drv:" & HWConfig.Motor5_DriveNo.ToString & " CH:" & HWConfig.Motor5_OutputNo.ToString)
        Debug.WriteLine("Motor6 Drv:" & HWConfig.Motor6_DriveNo.ToString & " CH:" & HWConfig.Motor6_OutputNo.ToString)
        Debug.WriteLine("Motor7 Drv:" & HWConfig.Motor7_DriveNo.ToString & " CH:" & HWConfig.Motor7_OutputNo.ToString)

        '---- DOF Translate & Operating Parameter ----

        ConfigLoad(True)

        CommandReceverTask.WorkerSupportsCancellation = True
        CommandReceverTask.WorkerReportsProgress = True
        BroadcastReceverTask.WorkerSupportsCancellation = True
        BroadcastReceverTask.WorkerReportsProgress = True
        WheelReceverTask.WorkerSupportsCancellation = True
        WheelReceverTask.WorkerReportsProgress = True

        'IO init
        'LCD MAP
        'Bit 0 - LCD RS (register select : 0-command 1-data)
        'Bit 1 - LCD RW (Read/Write : 0-write 1-read)
        'Bit 2 - LCD E  (enable : 0-ignore 1-data apply)
        'Bit 3 - LCD BL (backlight)
        'Bit 4 - LCD D4
        'Bit 5 - LCD D5
        'Bit 6 - LCD D6
        'Bit 7 - LCD D7
        Array.Clear(OpenGPIOPin, 0, OpenGPIOPin.Length)
        Array.Clear(I2CMap, 0, I2CMap.Length)
        Array.Clear(UseComm, 0, UseComm.Length)
        OpenGPIOPin(0) = True
        GPIO = Windows.Devices.Gpio.GpioController.GetDefault
        For i As Integer = 0 To GPIOAvailablePin.Length - 1
            GPIOPIN(i) = GPIO.OpenPin(GPIOAvailablePin(i))
            GPIOPIN(i).SetDriveMode(Windows.Devices.Gpio.GpioPinDriveMode.InputPullUp)
        Next
        'TestIO = GPIO.OpenPin(14)
        'TestLED.SetDriveMode(Windows.Devices.Gpio.GpioPinDriveMode.Output)
        GPIOPIN(StatusLED).SetDriveMode(Windows.Devices.Gpio.GpioPinDriveMode.Output)

        Init_I2C().Wait()
        I2COK = False '<--- Disable I2C
        If I2COK Then
            Debug.WriteLine("I2C OK")
            Init_LCD()
            LCDCommand(40) 'func set
            LCDCommand(15) 'display on
            LCDCommand(1)  'clear display
            LCDText(GetIPAddress, 0)
        End If


        Init_UART().Wait()
        'For i As Integer = 0 To CommWriter.Length - 1
        '    CommWriter(i) = New Windows.Storage.Streams.DataWriter
        'Next
        CMDRes.Clear()

        'run UDP recever in new task
        'Dim TRX As Task = Task.Run(Sub() UDPReceiver())
        CommandReceverTask.RunWorkerAsync(NetBinding)
        BroadcastReceverTask.RunWorkerAsync(NetBinding)
        WheelReceverTask.RunWorkerAsync()

        'run UART in new task
        Dim TUS As Task = Task.Run(Sub() UARTTransmitter())
        Dim TUR0 As Task = Task.Run(Sub() UARTReceiver(0))
        Dim TUR1 As Task = Task.Run(Sub() UARTReceiver(1))
        Dim TUR2 As Task = Task.Run(Sub() UARTReceiver(2))
        Dim TUR3 As Task = Task.Run(Sub() UARTReceiver(3))
        Dim TUR4 As Task = Task.Run(Sub() UARTReceiver(4))
        Dim TUR5 As Task = Task.Run(Sub() UARTReceiver(5))
        'Dim TUR6 As Task = Task.Run(Sub() UARTReceiver(6))
        'Dim TUW As Task = Task.Run(Sub() UARTReceiver_Wheel())
        Dim TIO As Task = Task.Run(Sub() GPIOTask())
        Dim TST As Task = Task.Run(Sub() StateControl())
        'Dim TUR2 As Task = Task.Run(Sub() BGUARTReceiver(2))
        'Dim TUR3 As Task = Task.Run(Sub() BGUARTReceiver(3))
        Dim TTX As Task = Task.Run(Sub() UDPTransmitter())
        Dim TGR As Task = Task.Run(Sub() GlobalUARTResponder())


        '
        ' If you start any asynchronous methods here, prevent the task
        ' from closing prematurely by using BackgroundTaskDeferral as
        ' described in http://aka.ms/backgroundtaskdeferral
        '

        'TestLED.Write(Windows.Devices.Gpio.GpioPinValue.High)
        GPIOPIN(StatusLED).Write(Windows.Devices.Gpio.GpioPinValue.High)
        LCDText("STARTUP...", 1)

        'TXBuffer = Ascii.GetBytes("READY")
        'NETService.SendAsync(TXBuffer, 5, PCEndpoint)
        'TRX.Wait()
        'TUS.Wait()

        Task.Delay(2000).Wait()

        '------ Profile 0 (gearbox) ------
        'ODrive.MotorPreset(0).Current_Limit = 40.0          'Default 10
        'ODrive.MotorPreset(0).Current_Limit_Tolerance = 1.5 'Default 1.25
        'ODrive.MotorPreset(0).Current_Range = 100.0         'Default 20
        'ODrive.MotorPreset(0).Velocity_Limit = ODrive.RPM_to_Pluse(3500.0, Axis0_PPR)
        'ODrive.MotorPreset(0).Acceleration_Limit = 400000.0
        'ODrive.MotorPreset(0).Deacceleration_Limit = 400000.0
        ''ODrive.SlowPreset(0).Velocity_Limit = ODrive.RPM_to_Pluse(500.0, Axis0_PPR)
        ''ODrive.SlowPreset(0).Acceleration_Limit = 10000.0
        ''ODrive.SlowPreset(0).Deacceleration_Limit = 10000.0
        'ODrive.MotorPreset(0).Position_Gain = 95.0
        'ODrive.MotorPreset(0).Velocity_Gain = 0.0011
        'ODrive.MotorPreset(0).Velocity_Integrator_Gain = 0.0002

        '------ Profile 1 (lead screw) ------
        'ODrive.MotorPreset(1).Current_Limit = 40.0
        'ODrive.MotorPreset(1).Current_Limit_Tolerance = 1.5
        'ODrive.MotorPreset(1).Current_Range = 100.0
        'ODrive.MotorPreset(1).Velocity_Limit = ODrive.RPM_to_Pluse(4400.0, Axis3_PPR)
        'ODrive.MotorPreset(1).Acceleration_Limit = 800000.0 ' <- old value is 1500000.0
        'ODrive.MotorPreset(1).Deacceleration_Limit = 800000.0
        ''ODrive.SlowPreset(1).Velocity_Limit = ODrive.RPM_to_Pluse(250.0, Axis3_PPR)
        ''ODrive.SlowPreset(1).Acceleration_Limit = 10000.0
        ''ODrive.SlowPreset(1).Deacceleration_Limit = 10000.0
        'ODrive.MotorPreset(1).Position_Gain = 95.0
        'ODrive.MotorPreset(1).Velocity_Gain = 0.0011
        'ODrive.MotorPreset(1).Velocity_Integrator_Gain = 0.0002

        '------ Profile 2 (wheel calibation) ------
        'ODrive.MotorPreset(2).Current_Limit = 10
        'ODrive.MotorPreset(2).Current_Limit_Tolerance = 1.25
        'ODrive.MotorPreset(2).Current_Range = 25.0
        ''ODrive.MotorPreset(2).Velocity_Limit = ODrive.RPM_to_Pluse(50.0, 1024)
        ''ODrive.MotorPreset(2).Acceleration_Limit = 800000.0
        ''ODrive.MotorPreset(2).Deacceleration_Limit = 800000.0
        'ODrive.MotorPreset(2).Position_Gain = 20.0
        'ODrive.MotorPreset(2).Velocity_Gain = 0.0005
        'ODrive.MotorPreset(2).Velocity_Integrator_Gain = 0.0

        '------ Profile 3 (wheel run) ------
        'ODrive.MotorPreset(3).Current_Limit = 40.0
        'ODrive.MotorPreset(3).Current_Limit_Tolerance = 1.25
        'ODrive.MotorPreset(3).Current_Range = 25
        'ODrive.MotorPreset(3).Velocity_Limit = ODrive.RPM_to_Pluse(600.0, 1024)
        'ODrive.MotorPreset(3).Acceleration_Limit = 800000.0 ' <- old value is 1500000.0
        'ODrive.MotorPreset(3).Deacceleration_Limit = 800000.0
        'ODrive.MotorPreset(3).Position_Gain = 20.0
        'ODrive.MotorPreset(3).Velocity_Gain = 0.0005
        'ODrive.MotorPreset(3).Velocity_Integrator_Gain = 0.0

        '---- motor profile selection ----
        'ODrive.MotorProfile(0) = 0
        'ODrive.MotorProfile(1) = 0
        'ODrive.MotorProfile(2) = 0
        'ODrive.MotorProfile(3) = 1
        'ODrive.MotorProfile(4) = 0
        'ODrive.MotorProfile(5) = 0

        Log.Data.DOF0_POS = "32768"
        Log.Data.DOF1_POS = "32768"
        Log.Data.DOF2_POS = "32768"
        Log.Data.DOF3_POS = "32768"
        Log.Data.DOF4_POS = "32768"
        Log.Data.DOF5_POS = "32768"

        'if axis not config display "drive Offlinr" on dashboard
        For i As Integer = 0 To Log.Data.Axis_StateTXT.Length - 1
            Select Case i
                Case 0 : If HWConfig.Motor0_DriveNo < 0 Or HWConfig.Motor0_OutputNo < 0 Then Log.Data.Axis_StateTXT(i) = "Drive Offline"
                Case 1 : If HWConfig.Motor1_DriveNo < 0 Or HWConfig.Motor1_OutputNo < 0 Then Log.Data.Axis_StateTXT(i) = "Drive Offline"
                Case 2 : If HWConfig.Motor2_DriveNo < 0 Or HWConfig.Motor2_OutputNo < 0 Then Log.Data.Axis_StateTXT(i) = "Drive Offline"
                Case 3 : If HWConfig.Motor3_DriveNo < 0 Or HWConfig.Motor3_OutputNo < 0 Then Log.Data.Axis_StateTXT(i) = "Drive Offline"
                Case 4 : If HWConfig.Motor4_DriveNo < 0 Or HWConfig.Motor4_OutputNo < 0 Then Log.Data.Axis_StateTXT(i) = "Drive Offline"
                Case 5 : If HWConfig.Motor5_DriveNo < 0 Or HWConfig.Motor5_OutputNo < 0 Then Log.Data.Axis_StateTXT(i) = "Drive Offline"
                Case 6 : If HWConfig.Motor6_DriveNo < 0 Or HWConfig.Motor6_OutputNo < 0 Then Log.Data.Axis_StateTXT(i) = "Drive Offline"
                Case 7 : If HWConfig.Motor7_DriveNo < 0 Or HWConfig.Motor7_OutputNo < 0 Then Log.Data.Axis_StateTXT(i) = "Drive Offline"
            End Select
        Next

        If Not HWConfError Then
            Drive_OK(0) = Drive_Connect(0) : Log.Data.Drive0_ON = 0
            Drive_OK(1) = Drive_Connect(1) : Log.Data.Drive1_ON = 0
            Drive_OK(2) = Drive_Connect(2) : Log.Data.Drive2_ON = 0
            Drive_OK(3) = Drive_Connect(3) : Log.Data.Drive3_ON = 0
            If Drive_OK(0) Then Drive_OK(0) = OdriveCheck(0)
            If Drive_OK(1) Then Drive_OK(1) = OdriveCheck(1)
            If Drive_OK(2) Then Drive_OK(2) = OdriveCheck(2)
            If Drive_OK(3) Then Drive_OK(3) = OdriveCheck(3)
            Control_OK = Control_Connect()
        End If
        'OPConfig.Auto_Start = 1
        If OPConfig.Auto_Start > 0 And HWConfError = False Then
            'Drive_OK(0) = Drive_Connect(0) : Log.Data.Drive0_ON = 0
            'Drive_OK(1) = Drive_Connect(1) : Log.Data.Drive1_ON = 0
            'Drive_OK(2) = Drive_Connect(2) : Log.Data.Drive2_ON = 0
            'Drive_OK(3) = Drive_Connect(3) : Log.Data.Drive3_ON = 0
            'If Drive_OK(0) Then Drive_OK(0) = OdriveCheck(0)
            'If Drive_OK(1) Then Drive_OK(1) = OdriveCheck(1)
            'If Drive_OK(2) Then Drive_OK(2) = OdriveCheck(2)
            'If Drive_OK(3) Then Drive_OK(3) = OdriveCheck(3)

            If System_Connect() Then
                SensorOK = True
                Enc_OK(0) = True
                Enc_OK(1) = True
                Enc_OK(2) = True
                Enc_OK(3) = True
                Enc_OK(4) = True
                Enc_OK(5) = True
                'ODrive.Motor(7).Enable = True                
            End If
        Else
            If Not HWConfError Then System_Connect()
        End If
        If Not HWConfError Then DisplayOK = Display_Connect()
        If DisplayOK Then
            Task.Delay(100).Wait()
            WriteNextion("")
            Task.Delay(100).Wait()
            WriteNextion("")
            Task.Delay(500).Wait()
            WriteNextion("page 1")
            NextionUpdate(1)
            Debug.WriteLine("display update")
        End If

        'If NetConfig.Telemetry_Mode = TelemetryMode.FTP Then
        '    FTP.FTPConnectAsync(NetConfig.FTPServer_IP, "anonymous", "").Wait()
        '    If FTP.Connected Then
        '        Debug.WriteLine("FTP connected")
        '        Log.Data.System_Event = "FTP Connected"
        '        FTP.FileCreate(Log.GetDateTimeName & "_xDash_00_Start.txt", False, False).Wait()
        '        Log.Clear()
        '    Else
        '        Debug.Write("FTP error : ")
        '        Debug.WriteLine(FTP.GetErrStr())
        '        Log.Data.System_Event = "FTP error : " & FTP.GetErrStr()
        '    End If
        'End If

        If SensorOK Then

            Dim SensorShadow(4) As Single
            Dim SNCount(4) As Single
            Const SensorT As Single = 2.5
            Dim ResCount As Integer = 0
            Dim ResCheck As Integer = 0
            Task.Delay(1000).Wait()
            CommandProcessor("[0]DRIVE(4).WRITE(H000000)") 'turn off all IR heater
            CommandProcessor("[0]DRIVE(4).WRITE(F111111)") 'set all ESC output to idle
            WriteNextion("page_control.va1.val=10")
            System.Array.Clear(SNCount, 0, SNCount.Length)

            ' --- read all gearbox encoder and check for error
            SysEvent("- Axis Encoder Check -")
            For i As Integer = 1 To 10
                CommandProcessor("[0]DRIVE(4).WRITE(E)")
                Task.Delay(250).Wait()
                ResCount = 0
                Debug.WriteLine(SensorShadow(0) & " " & SensorShadow(1) & " " & SensorShadow(2) & " " & SensorShadow(3))
                If Math.Abs(IORegister(Reg.Device_SystemIO, Reg.IO_X) - SensorShadow(0)) < SensorT Then ResCount += 1 : SNCount(0) += 1
                If Math.Abs(IORegister(Reg.Device_SystemIO, Reg.IO_Y) - SensorShadow(1)) < SensorT Then ResCount += 1 : SNCount(1) += 1
                If Math.Abs(IORegister(Reg.Device_SystemIO, Reg.IO_Z) - SensorShadow(2)) < SensorT Then ResCount += 1 : SNCount(2) += 1
                If Math.Abs(IORegister(Reg.Device_SystemIO, Reg.IO_A) - SensorShadow(3)) < SensorT Then ResCount += 1 : SNCount(3) += 1
                If SensorShadow(0) + SensorShadow(1) + SensorShadow(2) + SensorShadow(3) > 0.01 Then
                    If Math.Abs(IORegister(Reg.Device_SystemIO, Reg.IO_X) - SensorShadow(0)) > SensorT Then Enc_OK(0) = False
                    If Math.Abs(IORegister(Reg.Device_SystemIO, Reg.IO_Y) - SensorShadow(1)) > SensorT Then Enc_OK(1) = False
                    If Math.Abs(IORegister(Reg.Device_SystemIO, Reg.IO_Z) - SensorShadow(2)) > SensorT Then Enc_OK(2) = False
                    If Math.Abs(IORegister(Reg.Device_SystemIO, Reg.IO_A) - SensorShadow(3)) > SensorT Then Enc_OK(3) = False
                End If
                SensorShadow(0) = IORegister(Reg.Device_SystemIO, Reg.IO_X)
                SensorShadow(1) = IORegister(Reg.Device_SystemIO, Reg.IO_Y)
                SensorShadow(2) = IORegister(Reg.Device_SystemIO, Reg.IO_Z)
                SensorShadow(3) = IORegister(Reg.Device_SystemIO, Reg.IO_A)
                If ResCount = 4 Then
                    ResCheck += 1
                Else
                    ResCheck = 0
                End If
                If ResCheck > 3 Then Exit For
            Next

            'For i As Integer = 1 To 5
            '    CommandProcessor("[0]DRIVE(4).WRITE(A)")
            '    Task.Delay(250).Wait()
            '    Debug.Write("A0:" & IORegister(Reg.Device_SystemIO, Reg.IO_U))
            '    Debug.Write(" A1:" & IORegister(Reg.Device_SystemIO, Reg.IO_V))
            '    Debug.Write(" A2:" & IORegister(Reg.Device_SystemIO, Reg.IO_W))
            '    Debug.WriteLine(" A3:" & IORegister(Reg.Device_SystemIO, Reg.IO_B))
            'Next

            For i As Integer = 1 To 5
                CommandProcessor("[0]DRIVE(4).WRITE(R)")
                Task.Delay(250).Wait()
                Debug.Write("R:" & IORegister(Reg.Device_SystemIO, Reg.IO_R))
                Debug.Write(" S:" & IORegister(Reg.Device_SystemIO, Reg.IO_S))
                Debug.Write(" T:" & IORegister(Reg.Device_SystemIO, Reg.IO_T))
                Debug.WriteLine(" C:" & IORegister(Reg.Device_SystemIO, Reg.IO_C))
            Next

            'If ResCheck <= 3 Then
            '    Debug.WriteLine("sensor error")
            '    Enc1_OK = False
            '    Enc2_OK = False
            '    Enc3_OK = False
            '    Enc4_OK = False
            'End If

            'Dim AxisHome_DEG_0 As Short = 270.0
            'Dim AxisHome_DEG_1 As Short = 295.0
            'Dim AxisHome_DEG_2 As Short = 260.0
            'Dim AxisHome_DEG_3 As Short = 0.0


            If IORegister(Reg.Device_SystemIO, Reg.IO_Y) > 3000 Then
                IORegister(Reg.Device_SystemIO, Reg.IO_Y) -= 3000
                SysEvent("!!! --- Critical GPIO Subsystem Error --- !!!")
            End If

            Dim AxisHome_Dist_0 As Short = Math.Abs(AxisMath.GetCircleShortPath(IORegister(Reg.Device_SystemIO, Reg.IO_X), OPConfig.Axis_HomeDeg(0)))
            Dim AxisHome_Dist_1 As Short = Math.Abs(AxisMath.GetCircleShortPath(IORegister(Reg.Device_SystemIO, Reg.IO_Y), OPConfig.Axis_HomeDeg(1)))
            Dim AxisHome_Dist_2 As Short = Math.Abs(AxisMath.GetCircleShortPath(IORegister(Reg.Device_SystemIO, Reg.IO_Z), OPConfig.Axis_HomeDeg(2)))
            Dim AxisHome_Dist_3 As Short = Math.Abs(AxisMath.GetCircleShortPath(IORegister(Reg.Device_SystemIO, Reg.IO_A), OPConfig.Axis_HomeDeg(3)))

            If AxisHome_Dist_0 > OPConfig.Axis_HomeLimit(0) Or ConfigOK(0) = False Then Enc_OK(0) = False
            If AxisHome_Dist_1 > OPConfig.Axis_HomeLimit(1) Or ConfigOK(1) = False Then Enc_OK(1) = False
            If AxisHome_Dist_2 > OPConfig.Axis_HomeLimit(2) Or ConfigOK(2) = False Then Enc_OK(2) = False
            If AxisHome_Dist_3 > OPConfig.Axis_HomeLimit(3) Or ConfigOK(2) = False Then Enc_OK(3) = False

            If IORegister(Reg.Device_SystemIO, Reg.IO_X) < 0.01 Or SNCount(0) < 3 Then Enc_OK(0) = False
            If IORegister(Reg.Device_SystemIO, Reg.IO_Y) < 0.01 Or SNCount(1) < 3 Then Enc_OK(1) = False
            If IORegister(Reg.Device_SystemIO, Reg.IO_Z) < 0.01 Or SNCount(2) < 3 Then Enc_OK(2) = False
            If IORegister(Reg.Device_SystemIO, Reg.IO_A) < 0.01 Or SNCount(3) < 3 Then Enc_OK(3) = False

            Debug.WriteLine("M0:" & IORegister(Reg.Device_SystemIO, Reg.IO_X) & " SNR:" & SNCount(0) & " Dist:" & AxisHome_Dist_0 & " " & Enc_OK(0))
            Debug.WriteLine("M1:" & IORegister(Reg.Device_SystemIO, Reg.IO_Y) & " SNR:" & SNCount(1) & " Dist:" & AxisHome_Dist_1 & " " & Enc_OK(1))
            Debug.WriteLine("M2:" & IORegister(Reg.Device_SystemIO, Reg.IO_Z) & " SNR:" & SNCount(2) & " Dist:" & AxisHome_Dist_2 & " " & Enc_OK(2))
            Debug.WriteLine("M3:" & IORegister(Reg.Device_SystemIO, Reg.IO_A) & " SNR:" & SNCount(3) & " Dist:" & AxisHome_Dist_3 & " " & Enc_OK(3))

            Dim ManualOverride As Boolean = False

            ODrive.Motor(0).LimitOverride = ManualOverride
            ODrive.Motor(1).LimitOverride = ManualOverride
            ODrive.Motor(2).LimitOverride = ManualOverride
            ODrive.Motor(3).LimitOverride = ManualOverride

            'If ManualOverride Then
            '    'If Drive1_OK Then ODrive.Motor(3).Enable = Home_Linear(1, 1, 3, True)
            '    'If Drive0_OK Then ODrive.Motor(0).Enable = Home_Gear(0, 0, AxisHome_DEG_0, Axis0_PPR, 80.0, True, 0, True)
            '    'If Drive0_OK Then ODrive.Motor(1).Enable = Home_Gear(0, 1, AxisHome_DEG_1, Axis1_PPR, 80.0, False, 1, True)
            '    'If Drive1_OK Then ODrive.Motor(2).Enable = Home_Gear(1, 0, AxisHome_DEG_2, Axis2_PPR, 80.0, False, 2, True)
            'Else

            ' ---- UMU parameter review ----
            'For xrev As Integer = 0 To 3
            '    Debug.WriteLine("U" & xrev.ToString & " cal:" & ODrive.MotorPreset(Axis(xrev).CalibrationProfile).Velocity_Limit & " - " & ODrive.MotorPreset(Axis(xrev).CalibrationProfile).Trajectory_Velocity_Limit)
            '    Debug.WriteLine("U" & xrev.ToString & " run:" & ODrive.MotorPreset(Axis(xrev).RunProfile).Velocity_Limit & " - " & ODrive.MotorPreset(Axis(xrev).RunProfile).Trajectory_Velocity_Limit)
            '    Debug.WriteLine("U" & xrev.ToString & " prk:" & ODrive.MotorPreset(Axis(xrev).ParkProfile).Velocity_Limit & " - " & ODrive.MotorPreset(Axis(xrev).ParkProfile).Trajectory_Velocity_Limit)
            'Next


            ' ---- Auto Homing ----
            If AutoHome Then
                SysEvent("- Auto startup sequence begin -")
                Debug.Write("Drive[0] status : ") : If Drive_OK(0) Then Debug.WriteLine("PASS") Else Debug.WriteLine("FAIL")
                Debug.Write("Drive[1] status : ") : If Drive_OK(1) Then Debug.WriteLine("PASS") Else Debug.WriteLine("FAIL")
                Debug.Write("Drive[2] status : ") : If Drive_OK(2) Then Debug.WriteLine("PASS") Else Debug.WriteLine("FAIL")
                Debug.Write("Drive[3] status : ") : If Drive_OK(3) Then Debug.WriteLine("PASS") Else Debug.WriteLine("FAIL")
                Debug.WriteLine(" - Axis homing begin - ")
                For i As Integer = 0 To 5
                    If ConfigOK(i) And (OPConfig.Axis_HomeEnable(i) = False Or OPConfig.Axis_Type(i) > 1) Then
                        Enc_OK(i) = True
                        SysEvent("Axis Encoder " & i.ToString & " Ignore")
                    End If
                    If Enc_OK(i) And ODrive.Map_DRV(i) >= 0 Then
                        If Drive_OK(ODrive.Map_DRV(i)) Then
                            Select Case OPConfig.Axis_Type(i)
                                'Case ODrive.AxisType.Circular : ODrive.Motor(i).Enable = Home_Gear(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.Axis(i).HomeDeg, ODrive.Axis(i).HomeLimit, ODrive.Axis(i).HomeTotalance, ODrive.Axis(i).HomeTimeout, ODrive.Axis(i).IndexTimeout, ODrive.Axis(i).PPR, ODrive.Axis(i).GearRatio, ODrive.Axis(i).HomeDir, ODrive.Axis(i).CalibrationProfile, ODrive.Axis(i).RunProfile, Not ODrive.Axis(i).HomeEnable, ODrive.Axis(i).IndexEnable)
                                Case ODrive.AxisType.Circular : ODrive.Motor(i).Enable = Home_Gear(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.Axis(i))
                                'Case ODrive.AxisType.Circular : ODrive.Motor(i).Enable = True
                                Case ODrive.AxisType.Linear : ODrive.Motor(i).Enable = Home_Linear(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.Axis(i).HomeLimit, ODrive.Axis(i).HomeTotalance, ODrive.Axis(i).HomeTimeout, ODrive.Axis(i).IndexTimeout, ODrive.Axis(i).CalibrationProfile, ODrive.Axis(i).RunProfile, Not ODrive.Axis(i).HomeEnable)
                                Case ODrive.AxisType.Direct : ODrive.Motor(i).Enable = Home_Direct(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.Axis(i).RunProfile, ODrive.Axis(i).HomeEnc)
                                Case Else : ODrive.Motor(i).Enable = False
                            End Select
                        Else
                            SysEvent("Axis " & i.ToString & " mapping/type error")
                        End If
                    Else
                        SysEvent("Axis " & i.ToString & " not assign to motor")
                        ODrive.Motor(i).Enable = False
                    End If

                Next
                Debug.WriteLine(" - Axis homing finish - ")
                'Home_Gear_Multi()
                'Home_Gear_Multi_X()
                'If ConfigOK(0) And (OPConfig.Axis_HomeEnable(0) = False Or OPConfig.Axis_Type(0) > 1) Then Enc1_OK = True : Debug.WriteLine("Axis Encoder 0 Ignore")
                'If ConfigOK(1) And (OPConfig.Axis_HomeEnable(1) = False Or OPConfig.Axis_Type(1) > 1) Then Enc2_OK = True : Debug.WriteLine("Axis Encoder 1 Ignore")
                'If ConfigOK(2) And (OPConfig.Axis_HomeEnable(2) = False Or OPConfig.Axis_Type(2) > 1) Then Enc3_OK = True : Debug.WriteLine("Axis Encoder 2 Ignore")
                'If ConfigOK(3) And (OPConfig.Axis_HomeEnable(3) = False Or OPConfig.Axis_Type(3) > 1) Then Enc4_OK = True : Debug.WriteLine("Axis Encoder 3 Ignore")

                'If Enc1_OK And Drive_OK(ODrive.Map_DRV(0)) Then ODrive.Motor(0).Enable = Home_Gear(ODrive.Map_DRV(0), ODrive.Map_OUT(0), ODrive.Axis(0).HomeDeg, ODrive.Axis(0).PPR, ODrive.Axis(0).GearRatio, ODrive.Axis(0).HomeDir, ODrive.MotorProfile(0), Not ODrive.Axis(0).HomeEnable)
                'If Enc2_OK And Drive_OK(ODrive.Map_DRV(1)) Then ODrive.Motor(1).Enable = Home_Gear(ODrive.Map_DRV(1), ODrive.Map_OUT(1), ODrive.Axis(1).HomeDeg, ODrive.Axis(1).PPR, ODrive.Axis(1).GearRatio, ODrive.Axis(1).HomeDir, ODrive.MotorProfile(1), Not ODrive.Axis(1).HomeEnable)
                'If Enc3_OK And Drive_OK(ODrive.Map_DRV(2)) Then ODrive.Motor(2).Enable = Home_Gear(ODrive.Map_DRV(2), ODrive.Map_OUT(2), ODrive.Axis(2).HomeDeg, ODrive.Axis(2).PPR, ODrive.Axis(2).GearRatio, ODrive.Axis(2).HomeDir, ODrive.MotorProfile(2), Not ODrive.Axis(2).HomeEnable)
                'If Enc4_OK And Drive_OK(ODrive.Map_DRV(3)) Then ODrive.Motor(3).Enable = Home_Linear(ODrive.Map_DRV(3), ODrive.Map_OUT(3), ODrive.MotorProfile(2), False) 'center linear rail                   
                'ODrive.Motor(4).Enable = Home_Direct(ODrive.Map_DRV(4), ODrive.Map_OUT(4), 0, -344) 'center wheel

                Debug.WriteLine("- Axis Status -")
                For i As Integer = 0 To 5
                    If ODrive.Motor(i).Enable Then
                        Debug.WriteLine("Axis" & i.ToString & ":OK")
                    Else
                        Debug.WriteLine("Axis" & i.ToString & ":Disable")
                    End If
                Next

            End If
            'End If

            Dim OffsetStore As String = ""
            Dim OffsetMinus As String = ""
            OffsetMinus = "0"
            If ODrive.Motor(0).Enable Then
                If ControlRegister(Register.Axis0_Offset) < 0 Then OffsetMinus = "1"
                OffsetStore = OffsetStore & "S0" & OffsetMinus & Math.Abs(ControlRegister(Register.Axis0_Offset)).ToString("000000")
            Else
                OffsetStore = OffsetStore & "S00000000"
            End If
            OffsetStore = OffsetStore & " "


            CommandProcessor("[0]DRIVE(4).WRITE(" & OffsetStore & ")")
            OffsetStore = ""
            Task.Delay(25).Wait()

            OffsetMinus = "0"
            If ODrive.Motor(0).Enable Then
                If ControlRegister(Register.Axis1_Offset) < 0 Then OffsetMinus = "1"
                OffsetStore = OffsetStore & "S1" & OffsetMinus & Math.Abs(ControlRegister(Register.Axis1_Offset)).ToString("000000")
            Else
                OffsetStore = OffsetStore & "S10000000"
            End If
            OffsetStore = OffsetStore & " "

            CommandProcessor("[0]DRIVE(4).WRITE(" & OffsetStore & ")")
            OffsetStore = ""
            Task.Delay(25).Wait()

            OffsetMinus = "0"
            If ODrive.Motor(0).Enable Then
                If ControlRegister(Register.Axis2_Offset) < 0 Then OffsetMinus = "1"
                OffsetStore = OffsetStore & "S2" & OffsetMinus & Math.Abs(ControlRegister(Register.Axis2_Offset)).ToString("000000")
            Else
                OffsetStore = OffsetStore & "S20000000"
            End If
            OffsetStore = OffsetStore & " "

            CommandProcessor("[0]DRIVE(4).WRITE(" & OffsetStore & ")")
            OffsetStore = ""
            Task.Delay(25).Wait()

            OffsetMinus = "0"
            If ODrive.Motor(0).Enable Then
                If ControlRegister(Register.Axis3_Offset) < 0 Then OffsetMinus = "1"
                OffsetStore = OffsetStore & "S3" & OffsetMinus & Math.Abs(ControlRegister(Register.Axis3_Offset)).ToString("000000")
            Else
                OffsetStore = OffsetStore & "S30000000"
            End If
            OffsetStore = OffsetStore & " "

            CommandProcessor("[0]DRIVE(4).WRITE(" & OffsetStore & ")")
            OffsetStore = ""
            Task.Delay(25).Wait()

            OffsetStore = OffsetStore & "S70" & Math.Abs(AxisMath.GetCRC(ControlRegister(Register.Axis0_Offset), ControlRegister(Register.Axis1_Offset), ControlRegister(Register.Axis2_Offset), ControlRegister(Register.Axis3_Offset), 0, 0)).ToString("000000")

            CommandProcessor("[0]DRIVE(4).WRITE(" & OffsetStore & ")")
            OffsetStore = ""
            Task.Delay(25).Wait()


            'Debug.WriteLine(OffsetStore)

            WriteNextion("page_control.va1.val=0")
            MotorProfile_Shadow = 99
            StartupSequence_Finish = True
            MotorOutputControl(1, 1, False)
            SysEvent("- Auto startup sequence end -")

        End If
        SysEvent("- Startup sequence end -")
        Script_RUN = False
        'If ControlOK Then
        '    ControlRegister(Reg.Game_Controller_Type) = 0
        'End If

        While True
            Task.Delay(500).Wait()
        End While
        'LCDText("EXIT")
        'BGTask.Complete()
    End Sub

    Private Sub ConfigLoad(ColdStart As Boolean)
        Const TotalAxis As Integer = 8
        Const TotalProfile As Integer = 20
        Dim ConfRes As Boolean = False
        '---- DOF ( Axis Transfrom ) ----
        Dim DOFConfig As Storage.DOF_Config = Storage.LoadDOFConfig
        For i As Integer = 0 To 5
            AxisMath.DOF(i).Type = DOFConfig.DOF_Type(i)
            AxisMath.DOF(i).Axis0Percentage = DOFConfig.DOF_Axis0Percentage(i)
            AxisMath.DOF(i).Axis0DIR = DOFConfig.DOF_Axis0DIR(i)
            AxisMath.DOF(i).Axis1Percentage = DOFConfig.DOF_Axis1Percentage(i)
            AxisMath.DOF(i).Axis1DIR = DOFConfig.DOF_Axis1DIR(i)
            AxisMath.DOF(i).Axis2Percentage = DOFConfig.DOF_Axis2Percentage(i)
            AxisMath.DOF(i).Axis2DIR = DOFConfig.DOF_Axis2DIR(i)
            AxisMath.DOF(i).Axis3Percentage = DOFConfig.DOF_Axis3Percentage(i)
            AxisMath.DOF(i).Axis3DIR = DOFConfig.DOF_Axis3DIR(i)
            AxisMath.DOF(i).Axis4Percentage = DOFConfig.DOF_Axis4Percentage(i)
            AxisMath.DOF(i).Axis4DIR = DOFConfig.DOF_Axis4DIR(i)
            AxisMath.DOF(i).Axis5Percentage = DOFConfig.DOF_Axis5Percentage(i)
            AxisMath.DOF(i).Axis5DIR = DOFConfig.DOF_Axis5DIR(i)

            'Debug.WriteLine(" - ")
            'Debug.WriteLine(AxisMath.getTypeName(AxisMath.DOF(i).Type))
            'Debug.WriteLine("Axis 0 : " & AxisMath.getDIRName(AxisMath.DOF(i).Axis0DIR) & " : " & (DOFConfig.DOF_Axis0Percentage(i)))
            'Debug.WriteLine("Axis 1 : " & AxisMath.getDIRName(AxisMath.DOF(i).Axis1DIR) & " : " & (DOFConfig.DOF_Axis1Percentage(i)))
            'Debug.WriteLine("Axis 2 : " & AxisMath.getDIRName(AxisMath.DOF(i).Axis2DIR) & " : " & (DOFConfig.DOF_Axis2Percentage(i)))
            'Debug.WriteLine("Axis 3 : " & AxisMath.getDIRName(AxisMath.DOF(i).Axis3DIR) & " : " & (DOFConfig.DOF_Axis3Percentage(i)))
            'Debug.WriteLine("Axis 4 : " & AxisMath.getDIRName(AxisMath.DOF(i).Axis4DIR) & " : " & (DOFConfig.DOF_Axis4Percentage(i)))
            'Debug.WriteLine("Axis 5 : " & AxisMath.getDIRName(AxisMath.DOF(i).Axis5DIR) & " : " & (DOFConfig.DOF_Axis5Percentage(i)))

            Debug.WriteLine("DOF " & i.ToString & " : " & AxisMath.getTypeName(AxisMath.DOF(i).Type))
        Next
        'DOFConfigCheck()

        '---- Operation Parameter ----
        OPConfig = LoadOPConfig()
        Debug.WriteLine("Auto_Start : " & OPConfig.Auto_Start.ToString)
        Debug.WriteLine("Telemetry_Mode : " & OPConfig.Telemetry_Mode.ToString)
        Debug.WriteLine("Telemetry_Interval : " & OPConfig.Telemetry_Interval.ToString)
        Debug.WriteLine("Telemetry_Timeout : " & OPConfig.Telemetry_Timeout.ToString)
        Debug.WriteLine("Delay_ParameterSet : " & OPConfig.Delay_ParameterSet.ToString)
        Debug.WriteLine("Delay_StateChange : " & OPConfig.Delay_StateChange.ToString)

        For AxID As Integer = 0 To TotalAxis - 1
            'Debug.WriteLine("#" & AxID.ToString)
            'Debug.WriteLine("Axis_Type : " & OPConfig.Axis_Type(AxID))
            'Debug.WriteLine("Axis_IndexEnable : " & OPConfig.Axis_IndexEnable(AxID))
            'Debug.WriteLine("Axis_EndstopEnable : " & OPConfig.Axis_EndstopEnable(AxID))
            'Debug.WriteLine("Axis_EndstopOffset : " & OPConfig.Axis_EndstopOffset(AxID))
            'Debug.WriteLine("Axis_HomeEnable : " & OPConfig.Axis_HomeEnable(AxID))
            'Debug.WriteLine("Axis_HomeDeg : " & OPConfig.Axis_HomeDeg(AxID))
            'Debug.WriteLine("Axis_HomeDir : " & OPConfig.Axis_HomeDir(AxID))
            'Debug.WriteLine("Axis_HomeEnc : " & OPConfig.Axis_HomeEnc(AxID))
            'Debug.WriteLine("Axis_HomeLimit : " & OPConfig.Axis_HomeLimit(AxID))
            'Debug.WriteLine("Axis_HomeTotalance : " & OPConfig.Axis_HomeTotalance(AxID))
            'Debug.WriteLine("Axis_HomeTimeout : " & OPConfig.Axis_HomeTimeout(AxID))
            'Debug.WriteLine("Axis_IndexTimeout : " & OPConfig.Axis_IndexTimeout(AxID))
            'Debug.WriteLine("Axis_PPR : " & OPConfig.Axis_PPR(AxID))
            'Debug.WriteLine("Axis_RangePositive : " & OPConfig.Axis_RangePositive(AxID))
            'Debug.WriteLine("Axis_RangeNegative : " & OPConfig.Axis_RangeNegative(AxID))
            'Debug.WriteLine("Axis_GearRatio : " & OPConfig.Axis_GearRatio(AxID))
            'Debug.WriteLine("Axis_DefaultProfile : " & OPConfig.Axis_CalibrationProfile(AxID))
            'Debug.WriteLine("Axis_RunProfile : " & OPConfig.Axis_RunProfile(AxID))
            'Debug.WriteLine("Axis_ConfigOK : " & OPConfig.Axis_ConfigOK(AxID))

            If OPConfig.Axis_ConfigOK(AxID) Then
                If ColdStart Then
                    ODrive.Axis(AxID).Type = OPConfig.Axis_Type(AxID)
                    ODrive.Axis(AxID).IndexEnable = OPConfig.Axis_IndexEnable(AxID)
                    ODrive.Axis(AxID).EndstopEnable = OPConfig.Axis_EndstopEnable(AxID)
                    ODrive.Axis(AxID).EndstopOffset = OPConfig.Axis_EndstopOffset(AxID)
                    ODrive.Axis(AxID).HomeEnable = OPConfig.Axis_HomeEnable(AxID)
                    ODrive.Axis(AxID).HomeDeg = OPConfig.Axis_HomeDeg(AxID)
                    ODrive.Axis(AxID).HomeDir = OPConfig.Axis_HomeDir(AxID)
                    ODrive.Axis(AxID).HomeEnc = OPConfig.Axis_HomeEnc(AxID)
                    ODrive.Axis(AxID).HomeLimit = OPConfig.Axis_HomeLimit(AxID)
                    ODrive.Axis(AxID).HomeTotalance = OPConfig.Axis_HomeTotalance(AxID)
                    ODrive.Axis(AxID).HomeTimeout = OPConfig.Axis_HomeTimeout(AxID)
                    ODrive.Axis(AxID).IndexTimeout = OPConfig.Axis_IndexTimeout(AxID)
                    ODrive.Axis(AxID).PPR = OPConfig.Axis_PPR(AxID)
                    'ODrive.Axis(AxID).RangePositive = OPConfig.Axis_RangePositive(AxID)
                    'ODrive.Axis(AxID).RangeNegative = OPConfig.Axis_RangeNegative(AxID)
                    ODrive.Axis(AxID).GearRatio = OPConfig.Axis_GearRatio(AxID)
                    ODrive.Axis(AxID).CalibrationProfile = OPConfig.Axis_CalibrationProfile(AxID)
                    'ODrive.Axis(AxID).RunProfile = OPConfig.Axis_RunProfile(AxID)
                End If
                'Axis_PPD(AxID) = (ODrive.Axis(AxID).PPR * ODrive.Axis(AxID).GearRatio) / 360
                Select Case OPConfig.Axis_Type(AxID)
                    Case ODrive.AxisType.Circular
                        ODrive.Axis(AxID).RangePositive = Math.Abs(OPConfig.Axis_RangePositive(AxID))
                        ODrive.Axis(AxID).RangeNegative = Math.Abs(OPConfig.Axis_RangeNegative(AxID))
                        'TotalDistant = ODrive.Axis(AxID).RangePositive + ODrive.Axis(AxID).RangeNegative
                        'Const Axis0_PRange As Double = Axis0_PPD * (Axis0_RotationRange * 1)
                        'Const Axis0_Scale As Double = (Axis0_PRange / 100.0) / 2 '/ Input_Range
                        Axis_Scale(AxID) = Math.Max(ODrive.Axis(AxID).RangePositive, ODrive.Axis(AxID).RangeNegative) / 100.0
                        ODrive.Axis(AxID).RangeNegative = ODrive.Axis(AxID).RangeNegative * -1
                    Case ODrive.AxisType.Direct, ODrive.AxisType.Linear
                        ODrive.Axis(AxID).RangePositive = Math.Truncate(Math.Abs(OPConfig.Axis_RangePositive(AxID)))
                        ODrive.Axis(AxID).RangeNegative = Math.Truncate(Math.Abs(OPConfig.Axis_RangeNegative(AxID)))
                        Axis_Scale(AxID) = Math.Max(ODrive.Axis(AxID).RangePositive, ODrive.Axis(AxID).RangeNegative) / 100.0
                        ODrive.Axis(AxID).RangeNegative = ODrive.Axis(AxID).RangeNegative * -1
                    Case Else
                        OPConfig.Axis_ConfigOK(AxID) = False
                End Select
                Debug.WriteLine("Range : " & Axis_Scale(AxID).ToString)
                Debug.WriteLine("")
                ODrive.Axis(AxID).RunProfile = OPConfig.Axis_RunProfile(AxID)
            End If
            ODrive.Axis(AxID).ConfigOK = OPConfig.Axis_ConfigOK(AxID)
        Next
        For PxID As Integer = 0 To TotalProfile - 1

            'ODrive.MotorPreset(PxID).Safe_Velocity = 290.0 'default low speed (RPM)
            ODrive.MotorPreset(PxID).Safe_Velocity = 0.5 'default low speed (RPS)

            'Debug.WriteLine("#" & PxID.ToString)
            'Debug.WriteLine("Profile_CurrentLimitTolerance : " & OPConfig.Profile_CurrentLimitTolerance(PxID))
            'Debug.WriteLine("Profile_CurrentLimit : " & OPConfig.Profile_CurrentLimit(PxID))
            'Debug.WriteLine("Profile_CurrentRange : " & OPConfig.Profile_CurrentRange(PxID))
            'Debug.WriteLine("Profile_CurrentControlBandwidth : " & OPConfig.Profile_CurrentControlBandwidth(PxID))
            'Debug.WriteLine("Profile_VelocityLimit : " & OPConfig.Profile_VelocityLimit(PxID))
            'Debug.WriteLine("Profile_AccelerationLimit : " & OPConfig.Profile_AccelerationLimit(PxID))
            'Debug.WriteLine("Profile_DeaccelerationLimit : " & OPConfig.Profile_DeaccelerationLimit(PxID))
            'Debug.WriteLine("Profile_PositionGain : " & OPConfig.Profile_PositionGain(PxID))
            'Debug.WriteLine("Profile_VelocityGain : " & OPConfig.Profile_VelocityGain(PxID))
            'Debug.WriteLine("Profile_VelocityIntegratorGain : " & OPConfig.Profile_VelocityIntegratorGain(PxID))
            'Debug.WriteLine("Profile_CalibrationCurrent : " & OPConfig.Profile_CalibrationCurrent(PxID))
            'Debug.WriteLine("Profile_CalibrationVelocity : " & OPConfig.Profile_CalibrationVelocity(PxID))
            'Debug.WriteLine("Profile_CalibrationAcceleration : " & OPConfig.Profile_CalibrationAcceleration(PxID))
            'Debug.WriteLine("Profile_CalibrationRamp : " & OPConfig.Profile_CalibrationRamp(PxID))
            'Debug.WriteLine("Profile_ConfigOK : " & OPConfig.Profile_ConfigOK(PxID))
            'Debug.WriteLine("")

            If OPConfig.Profile_ConfigOK(PxID) Then
                ODrive.MotorPreset(PxID).Current_Limit_Tolerance = OPConfig.Profile_CurrentLimitTolerance(PxID)
                ODrive.MotorPreset(PxID).Current_Limit = OPConfig.Profile_CurrentLimit(PxID)
                ODrive.MotorPreset(PxID).Current_Range = OPConfig.Profile_CurrentRange(PxID)
                ODrive.MotorPreset(PxID).Current_Control_Bandwidth = OPConfig.Profile_CurrentControlBandwidth(PxID)
                ODrive.MotorPreset(PxID).Velocity_Limit = OPConfig.Profile_VelocityLimit(PxID)
                ODrive.MotorPreset(PxID).Trajectory_Velocity_Limit = OPConfig.Profile_TrajectoryVelocityLimit(PxID)
                ODrive.MotorPreset(PxID).Acceleration_Limit = OPConfig.Profile_AccelerationLimit(PxID)
                ODrive.MotorPreset(PxID).Deacceleration_Limit = OPConfig.Profile_DeaccelerationLimit(PxID)
                ODrive.MotorPreset(PxID).Position_Gain = OPConfig.Profile_PositionGain(PxID)
                ODrive.MotorPreset(PxID).Velocity_Gain = OPConfig.Profile_VelocityGain(PxID)
                ODrive.MotorPreset(PxID).Velocity_Integrator_Gain = OPConfig.Profile_VelocityIntegratorGain(PxID)
                ODrive.MotorPreset(PxID).Calibration_Current = OPConfig.Profile_CalibrationCurrent(PxID)
                ODrive.MotorPreset(PxID).Calibration_Velocity = OPConfig.Profile_CalibrationVelocity(PxID)
                ODrive.MotorPreset(PxID).Calibration_Acceleration = OPConfig.Profile_CalibrationAcceleration(PxID)
                ODrive.MotorPreset(PxID).Calibration_Ramp = OPConfig.Profile_CalibrationRamp(PxID)
            End If
            ODrive.MotorPreset(PxID).ConfigOK = OPConfig.Profile_ConfigOK(PxID)
        Next

        Debug.WriteLine("- Config Status -")
        For XxID As Integer = 0 To 7
            ConfRes = False
            If OPConfig.Axis_ConfigOK(XxID) Then
                If OPConfig.Profile_ConfigOK(OPConfig.Axis_RunProfile(XxID)) And OPConfig.Profile_ConfigOK(OPConfig.Axis_CalibrationProfile(XxID)) Then
                    ConfRes = True
                End If
            End If
            ConfigOK(XxID) = ConfRes
            If ConfRes Then
                Debug.WriteLine("Axis[" & XxID.ToString & "] OK")
            Else
                Debug.WriteLine("Axis[" & XxID.ToString & "] Incomplete")
                Log.Data.Axis_StateTXT(XxID) = "invalid config"
            End If
        Next

    End Sub

    Private Function DeviceExist(DeviceName As String) As Boolean
        Dim DFound As Boolean = False
        For devloop As Integer = 0 To UARTNameID.Length - 1
            If UARTNameID(devloop) <> "" Then
                If UARTNameID(devloop) = DeviceName Then
                    DFound = True
                    Exit For
                End If
            End If
        Next
        Return DFound
    End Function

    Private Async Function GetNetworkFolder(netAddress As String) As Task(Of Boolean)
        Dim zz As Windows.Storage.StorageFolder
        zz = Await Windows.Storage.StorageFolder.GetFolderFromPathAsync(netAddress)
        netStorage = zz
        Return True
    End Function


    Private Async Function Init_I2C() As Task(Of Boolean)
        I2CSetup = New Windows.Devices.I2c.I2cConnectionSettings(&H3F) ' PCF8754 i2c address is 20-27 hex
        I2CSetup.BusSpeed = Windows.Devices.I2c.I2cBusSpeed.StandardMode
        Dim i2cdevsel As String = Windows.Devices.I2c.I2cDevice.GetDeviceSelector("I2C1")
        Dim i2ccon = Await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(i2cdevsel)
        If i2ccon.Count > 0 Then
            I2CDevice = Await Windows.Devices.I2c.I2cDevice.FromIdAsync(i2ccon(0).Id, I2CSetup)
            I2COK = True
        Else
            I2COK = False
        End If
        Return True
    End Function


    Private Async Function Init_UART() As Task(Of Boolean)
        Dim uartdevsel As String = Windows.Devices.SerialCommunication.SerialDevice.GetDeviceSelector
        Dim uartcon = Await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(uartdevsel)
        System.Array.Clear(UARTNameID, 0, UARTNameID.Length)
        If uartcon.Count > 0 Then
            For i As Integer = 0 To uartcon.Count - 1
                UARTNameID(i) = uartcon(i).Id
            Next
        End If
        'UARTDevice = Await Windows.Devices.SerialCommunication.SerialDevice.FromIdAsync(uartcon(1).Id)
        Return True
    End Function

    Private Sub Init_LCD()
        I2CDevice.Write(New Byte() {&B0})
        Task.Delay(100).Wait()
        'dummy command (do nothing) .... use to fill 4 bit data gap at system startup 
        I2CDevice.Write(New Byte() {&B0})
        I2CDevice.Write(New Byte() {&B100})
        I2CDevice.Write(New Byte() {&B0})
        'set 4-bit mode
        I2CDevice.Write(New Byte() {&B100000})
        I2CDevice.Write(New Byte() {&B100100})
        I2CDevice.Write(New Byte() {&B100000})
        Task.Delay(10).Wait()
    End Sub

    Private Sub LCDCommand(Command As Byte)
        Dim HiNibble As Byte = Command And &B11110000
        Dim LoNibble As Byte = Command << 4
        I2CDevice.Write(New Byte() {HiNibble})
        I2CDevice.Write(New Byte() {HiNibble + &B100})
        I2CDevice.Write(New Byte() {HiNibble})
        I2CDevice.Write(New Byte() {LoNibble})
        I2CDevice.Write(New Byte() {LoNibble + &B100})
        I2CDevice.Write(New Byte() {LoNibble})
        Task.Delay(10).Wait()
    End Sub

    Private Sub LCDText(Text As String, Line As Integer)
        If I2COK Then
            Dim TxtArray() As Byte = Ascii.GetBytes(Text)
            Dim HiNibble(1) As Byte
            Dim LoNibble(1) As Byte
            Dim HiNibbleClk(1) As Byte
            Dim LoNibbleClk(1) As Byte
            If Line = 0 Then
                LCDCommand(2)
            Else
                LCDCommand(128 + 40)
            End If
            For i As Integer = 1 To TxtArray.Length
                If i > 15 Then Exit For
                If TxtArray(i - 1) = 0 Then Exit For
                HiNibble(1) = (TxtArray(i - 1) And &B11110000) + 1
                LoNibble(1) = (TxtArray(i - 1) << 4) + 1
                HiNibbleClk(1) = HiNibble(1) + &B100
                LoNibbleClk(1) = LoNibble(1) + &B100
                I2CDevice.Write(HiNibble)
                I2CDevice.Write(HiNibbleClk)
                I2CDevice.Write(HiNibble)
                I2CDevice.Write(LoNibble)
                I2CDevice.Write(LoNibbleClk)
                I2CDevice.Write(LoNibble)
            Next
        End If
    End Sub

    Private Sub BGeceiverA_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles CommandReceverTask.DoWork
        Dim RXL As Integer = 0
        Dim RXB(256) As Byte
        Dim IPBinding As CustomIPBinding = e.Argument
        Dim xRemoteEndPoint As New System.Net.IPEndPoint(System.Net.IPAddress.Any, 0)
        Dim NETRecever As New System.Net.Sockets.UdpClient(IPBinding.commandRXPort)
        Dim IPBindinf_A As Net.IPAddress = IPBinding.IPBinding_A
        Dim IPBindinf_B As Net.IPAddress = IPBinding.IPBinding_B
        Dim AnyIP As Boolean = False

        If IPBindinf_A.Equals(System.Net.IPAddress.Parse("127.0.0.1")) Then AnyIP = True

        SysEvent("Start - UDP Command Receiver")
        SysEvent("CMD-RX A [" & IPBindinf_A.ToString & ":" & IPBinding.commandRXPort.ToString & "]")
        SysEvent("CMD-RX B [" & IPBindinf_B.ToString & ":" & IPBinding.commandRXPort.ToString & "]")
        While True
            If CommandReceverTask.CancellationPending Then
                e.Cancel = True
                Exit While
            End If
            'RXL = NETRecever.Client.Receive(RXB)
            RXL = NETRecever.Client.ReceiveFrom(RXB, xRemoteEndPoint)
            CommandReturnPort = xRemoteEndPoint.Port
            If AnyIP Then CommandProcessor(Ascii.GetString(RXB).Substring(0, RXL))
            'Debug.WriteLine("UDP-RX " & xRemoteEndPoint.Address.ToString & ":" & xRemoteEndPoint.Port.ToString & " " & Ascii.GetString(RXB).Substring(0, RXL))
            If Not AnyIP Then
                If xRemoteEndPoint.Address.Equals(IPBindinf_A) Then CommandProcessor(Ascii.GetString(RXB).Substring(0, RXL))
                'If xRemoteEndPoint.Address.Equals(IPBindinf_B) Then CommandProcessor(Ascii.GetString(RXB).Substring(0, RXL))
            End If
        End While
        NETRecever.Dispose()
    End Sub

    Private Sub BGeceiverA_RunWorkerCompleted(ByVal sender As System.Object, ByVal e As RunWorkerCompletedEventArgs) Handles CommandReceverTask.RunWorkerCompleted

    End Sub

    Private Sub BGeceiverB_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles BroadcastReceverTask.DoWork
        Dim RXL As Integer = 0
        Dim RXB(256) As Byte
        Dim IPBinding As CustomIPBinding = e.Argument
        Dim xRemoteEndPoint As New System.Net.IPEndPoint(System.Net.IPAddress.Any, 0)
        Dim xRespondEndPoint As New System.Net.IPEndPoint(System.Net.IPAddress.Any, 0)
        Dim NETRecever As New System.Net.Sockets.UdpClient(IPBinding.broadcastRXPort)
        Dim NETTransmitter As New System.Net.Sockets.UdpClient()
        Dim broadcastRXString As String = ""
        Dim portIDX As Integer = -1
        Dim strIDX As Integer = -1
        Dim requestPort As Integer = -1
        Dim xMSGString() As Byte
        Dim RandomSource As New System.Random
        Dim OverrideName As String = ""
        Dim BoardcastName As String = System_Name

        SysEvent("Start - UDP Broadcast Receiver")
        If OverrideName <> "" Then BoardcastName = OverrideName
        While True
            If BroadcastReceverTask.CancellationPending Then
                e.Cancel = True
                Exit While
            End If
            'RXL = NETRecever.Client.Receive(RXB)
            RXL = NETRecever.Client.ReceiveFrom(RXB, xRemoteEndPoint)
            Try
                broadcastRXString = Ascii.GetString(RXB)
            Catch ex As Exception
                broadcastRXString = ""
            End Try
            If broadcastRXString.StartsWith("xSCANREQ") Then
                If broadcastRXString.Contains(",") Then
                    For Each bData As String In broadcastRXString.Split(",")
                        If bData.StartsWith("PORT:") Then Integer.TryParse(bData.Substring(bData.IndexOf(":") + 1), requestPort)
                    Next
                    If xRemoteEndPoint.AddressFamily.Equals(System.Net.Sockets.AddressFamily.InterNetwork) Then
                        If requestPort > 0 And requestPort < 65536 Then
                            Debug.WriteLine(xRemoteEndPoint.Address.ToString)
                            xMSGString = System.Text.Encoding.ASCII.GetBytes("xRES:" & BoardcastName & ",IP:" & IPBinding.LocalIP.ToString & ",PORT:" & IPBinding.commandRXPort.ToString & ",DOF:" & IPBinding.AUXData_MotorCount.ToString)
                            xRespondEndPoint.Address = xRemoteEndPoint.Address
                            xRespondEndPoint.Port = requestPort
                            Task.Delay(RandomSource.Next(10, 250)).Wait()
                            NETTransmitter.SendAsync(xMSGString, xMSGString.Length, xRespondEndPoint).Wait()
                        End If
                    End If
                End If
            End If
        End While
    End Sub

    Private Sub BGeceiverC_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs) Handles WheelReceverTask.DoWork
        Dim RXL As Integer = 0
        Dim RXB(256) As Byte
        Dim xRemoteEndPoint As New System.Net.IPEndPoint(System.Net.IPAddress.Any, 29000)
        'Dim xRespondEndPoint As New System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.168.148.219"), 12345)
        Dim NETRecever As New System.Net.Sockets.UdpClient(xRemoteEndPoint)
        'Dim NETTransmitter As New System.Net.Sockets.UdpClient()
        SysEvent("Start - UDP Wheel Receiver")
        While True
            If WheelReceverTask.CancellationPending Then
                e.Cancel = True
                Exit While
            End If
            'RXL = NETRecever.Client.Receive(RXB)
            RXL = NETRecever.Client.ReceiveFrom(RXB, xRemoteEndPoint)
            'If RXL > 0 Then NETTransmitter.SendAsync(RXB, RXL, xRespondEndPoint).Wait()
            If RXL > 3 Then
                If RXB(0) = 87 And RXB(1) = 72 Then
                    Select Case RXB(2)
                        Case 0
                            Wheel.WheelPOS = (RXB(3) * 256) + RXB(4)
                            Wheel.OprState = 0
                            Wheel.AnalogCH_1 = RXB(5)
                            Wheel.AnalogCH_2 = RXB(6)
                            Wheel.AnalogCH_3 = RXB(7)
                            Wheel.AnalogCH_4 = RXB(8)
                            Wheel.Button = (RXB(9) * 256) + RXB(10)
                        Case 1
                            Wheel.OprState = 1
                            DriveError(Reg.Device_Wheel).General = (RXB(3) * 256) + RXB(4)
                            DriveError(Reg.Device_Wheel).Motor = (RXB(5) * 256) + RXB(6)
                            DriveError(Reg.Device_Wheel).Encoder = (RXB(7) * 256) + RXB(8)
                            Wheel.Button = (RXB(9) * 256) + RXB(10)
                        Case Else
                            Debug.Write(" " & RXB(0).ToString)
                    End Select
                End If

            End If
            'Debug.WriteLine("WHEEL_DATA " & RXL)
        End While
    End Sub

    'Private Sub UDPReceiver(ByVal IPBinding_A As Net.IPAddress, ByVal IPBinding_B As Net.IPAddress, ByVal PortBinding As Integer)
    '    Dim RXL As Integer = 0
    '    Dim RXB(256) As Byte
    '    Dim RemoteEndPoint As New System.Net.IPEndPoint(System.Net.IPAddress.Any, 0)
    '    Dim NETRecever As New System.Net.Sockets.UdpClient(PortBinding)
    '    'Dim RXA As New System.Net.IPEndPoint(System.Net.IPAddress.Any, 0)
    '    Debug.WriteLine("UDP Receiver")
    '    'NETRecever.Client.SetSocketOption(Net.Sockets.SocketOptionLevel.IP,)
    '    Do Until TaskCancle
    '        'RXL = NETRecever.Available
    '        'If RXL > 0 Then
    '        ' If RXL > RXBuffer.Length Then RXL = RXBuffer.Length - 1
    '        '  NETRecever.Client.Receive(RXBuffer)
    '        'NETService.SendAsync(RXBuffer, RXL, PCEndpoint)
    '        '   CommandProcessor(Ascii.GetString(RXBuffer).Substring(0, RXL))
    '        'Debug.WriteLine("Data!")
    '        'End If
    '        'Task.Delay(1).Wait() 
    '        RXL = NETRecever.Client.Receive(RXB)
    '        Debug.WriteLine(NETRecever.Client.RemoteEndPoint.AddressFamily.ToString)

    '        ' Debug.WriteLine(ControlEndpoint.r)
    '        If Not Script_RUN Then CommandProcessor(Ascii.GetString(RXB).Substring(0, RXL))
    '    Loop
    'End Sub

    Private Async Sub UDPTransmitter()
        Dim TXBuffer(256) As Byte
        Dim TXLen As Integer = 0
        Dim TXPoint As Integer = 0
        Dim i As Integer = 0
        Dim WriteEvent As Boolean = False
        SysEvent("Start - UDP Transmitter")
        Do Until TaskCancle
            For i = 0 To 1
                If RespondReadIndex(i) <> RespondWriteIndex(i) Then
                    RespondReadIndex(i) += 1
                    If RespondReadIndex(i) >= RespondBufferSize Then RespondReadIndex(i) = 0
                    'If RespondBuffer(i, RespondReadIndex(i)) IsNot vbNullString Then
                    '    TXBuffer = Ascii.GetBytes(RespondBuffer(i, RespondReadIndex(i)))
                    '    TXLen = RespondBuffer(i, RespondReadIndex(i)).Length
                    '    If i = 0 Then NETTransmitter.SendAsync(TXBuffer, TXLen, PCEndpoint)
                    '    If i = 1 Then NETTransmitter.SendAsync(TXBuffer, TXLen, DashboardEndpoint)
                    'End If
                    TXPoint = RespondReadIndex(i) * (RespondPacketSize + RespondPacketSizeIndicator)
                    If i = 1 Then TXPoint += RespondByteMaxSize \ 2
                    TXLen = RespondByteBuffer(TXPoint + RespondPacketSize)
                    If TXLen > 0 Then
                        System.Array.Copy(RespondByteBuffer, TXPoint, TXBuffer, 0, TXLen)
                        'Debug.Write(" !" & TXPoint.ToString & "-" & TXLen)
                        WriteEvent = True
                        If i = 0 Then
                            Await NETTransmitter.SendAsync(TXBuffer, TXLen, PCEndpoint) ': Debug.Write(" ->" & System.Text.Encoding.ASCII.GetString(TXBuffer, 0, TXLen))
                            'Debug.Write("UDP-TX " & PCEndpoint.Address.ToString & ":" & PCEndpoint.Port.ToString & " " & System.Text.Encoding.ASCII.GetString(TXBuffer, 0, TXLen))
                        End If
                        If i = 1 Then Await NETTransmitter.SendAsync(TXBuffer, TXLen, DashboardEndpoint) ': Debug.Write(" >>" & System.Text.Encoding.ASCII.GetString(TXBuffer, 0, TXLen))
                    Else
                        ' Debug.Write(" <>" & System.Text.Encoding.ASCII.GetString(TXBuffer, 0, 64))
                    End If
                End If
            Next
            If Not WriteEvent Then Task.Delay(1).Wait()
            WriteEvent = False
        Loop
    End Sub


    Private Async Sub UARTTransmitter()

        Const LowPiorityTelemetryCNT = 20 'Range 1-99

        Dim TSequenceSTR(100) As String
        Dim TSequenceIDX(100) As Integer
        'Dim ASequenceSTR(100) As String
        Dim TSequencePIO(100) As Integer
        Dim TCnt As Integer = 0
        Dim RateCounter(10) As Integer
        Dim RCShadow(10) As Integer
        Dim TransmitEvent As Boolean = False
        Dim TelemetryTimeout As Integer = OPConfig.Telemetry_Timeout
        Const TelemetryCMDCount As Integer = 7
        Dim xString(100) As String
        Dim xWheelUDP As New System.Net.Sockets.UdpClient
        Dim WheelEndpoint As New System.Net.IPEndPoint(System.Net.IPAddress.Parse("192.168.148.191"), 54321)

        SysEvent("Start - UART TX TASK")

        For i As Integer = 0 To TSequencePIO.Length - 1
            TSequencePIO(i) = 3 'High piority telemetry command end index
        Next
        For i As Integer = LowPiorityTelemetryCNT To TSequencePIO.Length - 1
            TSequencePIO(i) = 7 'Low piority telemetry command end index
        Next

        'ACTUAL_POSITION_M0 
        TSequenceSTR(0) = "f 0" & ASCIIlineFeed
        'MOTOR_CURRENT_M0
        TSequenceSTR(1) = "r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, 0) & ASCIIlineFeed
        'ACTUAL_POSITION_M1 
        TSequenceSTR(2) = "f 1" & ASCIIlineFeed
        'MOTOR_CURRENT_M1 
        TSequenceSTR(3) = "r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, 1) & ASCIIlineFeed

        'ERROR_M0 
        TSequenceSTR(4) = "r " & ODrive.CommandString(ODrive.GENERAL_ERROR, 0) & ASCIIlineFeed
        'ERROR_M1
        TSequenceSTR(5) = "r " & ODrive.CommandString(ODrive.GENERAL_ERROR, 1) & ASCIIlineFeed
        'TEMPERATURE_M0
        TSequenceSTR(6) = "r " & ODrive.CommandString(ODrive.FET_TEMPERATURE, 0) & ASCIIlineFeed
        'TEMPERATURE_M1
        TSequenceSTR(7) = "r " & ODrive.CommandString(ODrive.FET_TEMPERATURE, 1) & ASCIIlineFeed

        'ASequenceSTR(0) = "A"
        'ASequenceSTR(1) = ""
        'ASequenceSTR(2) = "A"
        'ASequenceSTR(3) = ""

        Do Until TaskCancle
            TransmitEvent = False
            If Not CMDBUSY Then
                For i As Integer = 0 To CommWriter.Length
                    If UseComm(i) Or i = Reg.Device_Wheel Then


                        'If i = 3 Then
                        '  NextionWDT += 1
                        '  If NextionWDT > 500 Then
                        '    NextionWDT = 0
                        '  End If
                        'End If
                        If TelemetryEnable And Script_RUN = False Then
                            'If ControlRegister(Reg.Game_Controller_Type) = 0 And ODriveWheel_ID = i Then
                            '    If StreamMode(i, 0) Then
                            'CommWriter(i).WriteString("j 0")
                            'CommWriter(i).WriteString(ASCIIlineFeed)
                            '    End If
                            '    If StreamMode(i, 1) Then
                            '        CommWriter(i).WriteString("j 1")
                            '        CommWriter(i).WriteString(ASCIIlineFeed)
                            '    End If
                            'StreamLock(i) = True
                            'Else
                            'If i = 1 Then
                            'If i = Telemetry_DRV_Select Then
                            '    Select Case TelemetryIndex
                            '        Case 0
                            '            'CommWriter(i).WriteString("f " & Telemetry_AXIS_Select.ToString & ASCIIlineFeed)
                            '            WriteCommand(i, "f " & Telemetry_AXIS_Select.ToString & ASCIIlineFeed)
                            '            StreamLock(i) = True
                            '        Case 1
                            '            'CommWriter(i).WriteString("r " & ODrive.CommandString(ODrive.COMMAND_CURRENT, Telemetry_AXIS_Select) & ASCIIlineFeed)
                            '            WriteCommand(i, "r " & ODrive.CommandString(ODrive.COMMAND_CURRENT, Telemetry_AXIS_Select) & ASCIIlineFeed)
                            '            StreamLock(i) = True
                            '        Case 2
                            '            'CommWriter(i).WriteString("r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, Telemetry_AXIS_Select) & ASCIIlineFeed)
                            '            WriteCommand(i, "r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, Telemetry_AXIS_Select) & ASCIIlineFeed)
                            ''            StreamLock(i) = True
                            'End Select
                            'If CommWriter(0).StoreAsync Then

                            'End If
                            'If i = 0 Then
                            '        Select Case TelemetryIndex
                            '            Case 0
                            '                If ODrive.Motor(0).Enable Then
                            '                    WriteCommand(0, "f 0" & ASCIIlineFeed)
                            '                    StreamLock(0) = True
                            '                End If
                            '            Case 1
                            '                If ODrive.Motor(0).Enable Then
                            '                    WriteCommand(0, "r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, 0) & ASCIIlineFeed)
                            '                    StreamLock(0) = True
                            '                End If
                            '            Case 2
                            '                If ODrive.Motor(1).Enable Then
                            '                    WriteCommand(0, "f 1" & ASCIIlineFeed)
                            '                    StreamLock(0) = True
                            '                End If
                            '            Case 3
                            '                If ODrive.Motor(1).Enable Then
                            '                    WriteCommand(0, "r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, 1) & ASCIIlineFeed)
                            '                    StreamLock(0) = True
                            '                End If
                            '        End Select
                            '    End If
                            '    If i = 1 Then
                            '        Select Case TelemetryIndex
                            '            Case 0
                            '                If ODrive.Motor(2).Enable Then
                            '                    WriteCommand(1, "f 0" & ASCIIlineFeed)
                            '                    StreamLock(1) = True
                            '                End If
                            '            Case 1
                            '                If ODrive.Motor(2).Enable Then
                            '                    WriteCommand(1, "r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, 0) & ASCIIlineFeed)
                            '                    StreamLock(1) = True
                            '                End If
                            '            Case 2
                            '                If ODrive.Motor(3).Enable Then
                            '                    WriteCommand(1, "f 1" & ASCIIlineFeed)
                            '                    StreamLock(1) = True
                            '                End If
                            '            Case 3
                            '                If ODrive.Motor(3).Enable Then
                            '                    WriteCommand(1, "r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, 1) & ASCIIlineFeed)
                            '                    StreamLock(1) = True
                            '                End If
                            '        End Select
                            '    End If


                            'End If
                            'End If
                            'End If
                            'If RingWriteIndex(0) = RingReadIndex(0) Then
                            '    'If CommWriter(0).UnstoredBufferLength = 0 And COMMBusy(0) = False Then
                            '    TelemetryIdleCount(0) += 1
                            '    If TelemetryIdleCount(0) > BufferEmptyWait Then
                            '        TelemetryIdleCount(0) = 0
                            '        If ODrive.Motor(0).Enable And TelemetryIndex(0) < 6 Then
                            '            'If TelemetryCounter(0) = 0 Then WriteCommand(0, TSequenceSTR(TelemetryIndex(0)))
                            '            'If TelemetryCounter(0) = 0 And TSequenceSTR(TelemetryIndex(0)) <> "" Then CommWriter(0).WriteString(TSequenceSTR(TelemetryIndex(0))) : COMMBusy(0) = True
                            '            If TelemetryCounter(0) = 0 And TSequenceSTR(TelemetryIndex(0)) <> "" Then WriteCommand(0, TSequenceSTR(TelemetryIndex(0)))
                            '        End If
                            '        If ODrive.Motor(1).Enable And TelemetryIndex(0) > 5 Then
                            '            'If TelemetryCounter(0) = 0 Then WriteCommand(0, TSequenceSTR(TelemetryIndex(0)))
                            '            'If TelemetryIndex(0) > 3 Then TelemetryIndex(0) = 3
                            '            'If TelemetryCounter(0) = 0 And TSequenceSTR(TelemetryIndex(0)) <> "" Then CommWriter(0).WriteString(TSequenceSTR(TelemetryIndex(0))) : COMMBusy(0) = True
                            '            If TelemetryCounter(0) = 0 And TSequenceSTR(TelemetryIndex(0)) <> "" Then WriteCommand(0, TSequenceSTR(TelemetryIndex(0)))
                            '        End If
                            '        TelemetryCounter(0) += 1
                            '        If TelemetryCounter(0) > TelemetryTimeout Then
                            '            Debug.WriteLine("timeout 0:" & TelemetryIndex(0).ToString)
                            '            TelemetryCounter(0) = 0
                            '            TelemetryIndex(0) += 1
                            '            If TelemetryIndex(0) > 11 Then TelemetryIndex(0) = 0
                            '        End If
                            '    End If
                            'Else
                            '    TelemetryIdleCount(0) = 0
                            'End If

                            'If RingWriteIndex(1) = RingReadIndex(1) Then
                            '    'If CommWriter(1).UnstoredBufferLength = 0 And COMMBusy(1) = False Then
                            '    TelemetryIdleCount(1) += 1
                            '    If TelemetryIdleCount(1) > BufferEmptyWait Then
                            '        TelemetryIdleCount(1) = 0
                            '        If ODrive.Motor(2).Enable And TelemetryIndex(1) < 2 Then
                            '            'If TelemetryCounter(1) = 0 Then WriteCommand(1, TSequenceSTR(TelemetryIndex(1)))
                            '            'If TelemetryCounter(1) = 0 And TSequenceSTR(TelemetryIndex(1)) <> "" Then CommWriter(1).WriteString(TSequenceSTR(TelemetryIndex(1))) : COMMBusy(1) = True
                            '            If TelemetryCounter(1) = 0 And TSequenceSTR(TelemetryIndex(1)) <> "" Then WriteCommand(1, TSequenceSTR(TelemetryIndex(1)))
                            '        End If
                            '        If ODrive.Motor(3).Enable And TelemetryIndex(1) > 1 Then
                            '            'If TelemetryCounter(1) = 0 Then WriteCommand(1, TSequenceSTR(TelemetryIndex(1)))
                            '            'If TelemetryIndex(1) > 3 Then TelemetryIndex(1) = 3
                            '            'If TelemetryCounter(1) = 0 And TSequenceSTR(TelemetryIndex(1)) <> "" Then CommWriter(1).WriteString(TSequenceSTR(TelemetryIndex(1))) : COMMBusy(1) = True
                            '            If TelemetryCounter(1) = 0 And TSequenceSTR(TelemetryIndex(1)) <> "" Then WriteCommand(1, TSequenceSTR(TelemetryIndex(1)))
                            '        End If
                            '        TelemetryCounter(1) += 1
                            '        If TelemetryCounter(1) > TelemetryTimeout Then
                            '            Debug.WriteLine("timeout 1:" & TelemetryIndex(1).ToString)
                            '            TelemetryCounter(1) = 0
                            '            TelemetryIndex(1) += 1
                            '            If TelemetryIndex(1) > 11 Then TelemetryIndex(1) = 0
                            '        End If
                            '    End If
                            'Else
                            '    TelemetryIdleCount(1) = 0
                            'End If

                            If i < 4 Then
                                If Telemetry_Request_ID(i) <> RCShadow(i) Then
                                    RCShadow(i) = Telemetry_Request_ID(i)
                                    RateCounter(i) = 0
                                    If Telemetry_Request_ID(i) > TelemetryCMDCount Then Telemetry_Request_ID(i) = 0
                                    WriteCommand(i, TSequenceSTR(Telemetry_Request_ID(i)))
                                End If

                                RateCounter(i) += 1
                                If RateCounter(i) > TelemetryTimeout Then
                                    RateCounter(i) = 0
                                    Debug.WriteLine("Timeout (" & i.ToString & ")" & Telemetry_Request_ID(i).ToString)
                                    Telemetry_Request_ID(i) += 1
                                    If Telemetry_Request_ID(i) > TelemetryCMDCount Then Telemetry_Request_ID(i) = 0
                                End If
                            End If

                            'If Telemetry_Request_ID <> RCShadow Then
                            '    RCShadow = Telemetry_Request_ID
                            '    RateCounter = 0
                            '    'RCShadow = RateCounter
                            '    'Telemetry_Request_ID += 1
                            '    If Telemetry_Request_ID > 15 Then
                            '        Telemetry_Request_ID = 0
                            '    End If
                            '    Select Case Telemetry_Request_ID
                            '        Case 0
                            '            If ODrive.Motor(0).Enable Then
                            '                WriteCommand(0, "f 0" & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 1
                            '            If ODrive.Motor(1).Enable Then
                            '                WriteCommand(0, "f 1" & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 2
                            '            If ODrive.Motor(2).Enable Then
                            '                WriteCommand(1, "f 0" & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 3
                            '            If ODrive.Motor(3).Enable Then
                            '                WriteCommand(1, "f 1" & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 4
                            '            If ODrive.Motor(4).Enable Then
                            '                WriteCommand(2, "f 0" & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 5
                            '            If ODrive.Motor(5).Enable Then
                            '                WriteCommand(2, "f 1" & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 6
                            '            If ODrive.Motor(6).Enable Then
                            '                WriteCommand(3, "f 0" & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 7
                            '            If ODrive.Motor(7).Enable Then
                            '                WriteCommand(3, "f 1" & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 8
                            '            If ODrive.Motor(0).Enable Then
                            '                WriteCommand(0, "r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, 0) & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 9
                            '            If ODrive.Motor(1).Enable Then
                            '                WriteCommand(0, "r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, 1) & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 10
                            '            If ODrive.Motor(2).Enable Then
                            '                WriteCommand(1, "r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, 0) & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 11
                            '            If ODrive.Motor(3).Enable Then
                            '                WriteCommand(1, "r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, 1) & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 12
                            '            If ODrive.Motor(4).Enable Then
                            '                WriteCommand(2, "r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, 0) & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 13
                            '            If ODrive.Motor(5).Enable Then
                            '                WriteCommand(2, "r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, 1) & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 14
                            '            If ODrive.Motor(6).Enable Then
                            '                WriteCommand(3, "r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, 0) & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 15
                            '            If ODrive.Motor(7).Enable Then
                            '                WriteCommand(3, "r " & ODrive.CommandString(ODrive.MEASURED_CURRENT, 1) & ASCIIlineFeed)
                            '            Else
                            '                Telemetry_Request_ID += 1
                            '            End If
                            '        Case 16 : WriteCommand(0, ODrive.CommandString(ODrive.GENERAL_ERROR, 0) & ASCIIlineFeed)
                            '        Case 17 : WriteCommand(0, ODrive.CommandString(ODrive.GENERAL_ERROR, 1) & ASCIIlineFeed)
                            '        Case 18 : WriteCommand(1, ODrive.CommandString(ODrive.GENERAL_ERROR, 0) & ASCIIlineFeed)
                            '        Case 19 : WriteCommand(1, ODrive.CommandString(ODrive.GENERAL_ERROR, 1) & ASCIIlineFeed)
                            '        Case 20 : WriteCommand(2, ODrive.CommandString(ODrive.GENERAL_ERROR, 0) & ASCIIlineFeed)
                            '        Case 21 : WriteCommand(2, ODrive.CommandString(ODrive.GENERAL_ERROR, 1) & ASCIIlineFeed)
                            '        Case 22 : WriteCommand(3, ODrive.CommandString(ODrive.GENERAL_ERROR, 0) & ASCIIlineFeed)
                            '        Case 23 : WriteCommand(3, ODrive.CommandString(ODrive.GENERAL_ERROR, 1) & ASCIIlineFeed)
                            '    End Select
                            '    If Telemetry_Request_ID > 15 Then Telemetry_Request_ID = 0
                            'End If


                        End If


                        '--- load cell reader ----
                        'If Script_RUN = False Then
                        '    If RingWriteIndex(Reg.Device_SystemIO) = RingReadIndex(Reg.Device_SystemIO) Then
                        '        TelemetryIdleCount(Reg.Device_SystemIO) += 1
                        '        If TelemetryIdleCount(Reg.Device_SystemIO) > 20 Then
                        '            TelemetryIdleCount(Reg.Device_SystemIO) = 0
                        '            'If ASequenceSTR(TelemetryIndex(2)) <> "" Then CommWriter(2).WriteString(ASequenceSTR(TelemetryIndex(2)))
                        '            If ASequenceSTR(TelemetryIndex(2)) <> "" Then WriteCommand(Reg.Device_SystemIO, ASequenceSTR(TelemetryIndex(Reg.Device_SystemIO)))
                        '            TelemetryIndex(Reg.Device_SystemIO) += 1
                        '            If TelemetryIndex(Reg.Device_SystemIO) > 4 Then TelemetryIndex(Reg.Device_SystemIO) = 0
                        '        End If
                        '    Else
                        '        TelemetryIdleCount(Reg.Device_SystemIO) = 0
                        '    End If
                        'End If

                        'COMMBusy(i) = False

                        If RingWriteIndex(i) <> RingReadIndex(i) Then
                            TransmitEvent = True
                            RingReadIndex(i) += 1
                            If RingReadIndex(i) >= RingBufferSize Then RingReadIndex(i) = 0
                            Select Case i
                                Case Reg.Device_Display : CommWriter(i).WriteBytes(RingByteBuffer(i, RingReadIndex(i)))
                                Case Reg.Device_Wheel
                                    Await xWheelUDP.SendAsync(RingByteBuffer(i, RingReadIndex(i)), 7, WheelEndpoint)
                                    'CommWriter(i).WriteBytes(RingByteBuffer(i, RingReadIndex(i)))
                                    'xString(0) = Convert.ToString(RingByteBuffer(i, RingReadIndex(i))(3), 2).PadLeft(8, "0"c)
                                    'xString(1) = Convert.ToString(RingByteBuffer(i, RingReadIndex(i))(4), 2).PadLeft(8, "0"c)
                                    'xString(2) = Convert.ToString(RingByteBuffer(i, RingReadIndex(i))(5), 2).PadLeft(8, "0"c)
                                    'xString(3) = Convert.ToString(RingByteBuffer(i, RingReadIndex(i))(6), 2).PadLeft(8, "0"c)
                                    'Debug.WriteLine(xString(0) & " " & xString(1) & " " & xString(2) & " " & xString(3))
                                Case Else : CommWriter(i).WriteString(RingBuffer(i, RingReadIndex(i)))
                            End Select
                            If UseComm(i) Then Await CommWriter(i).StoreAsync()
                        End If


                        'Try
                        '    If RingINIndex(i) <> RingOUTIndex(i) Then
                        '        RingOUTIndex(i) += 1
                        '        If RingOUTIndex(i) > RingSize Then RingOUTIndex(i) = 0
                        '        CommWriter(i).WriteString(RingBuffer(i, RingOUTIndex(i)))
                        '        Await CommWriter(i).StoreAsync()
                        '    End If
                        '    'Await UARTDevice(i).OutputStream.WriteAsync(CommWriter(i).DetachBuffer)

                        '    'If i = 3 Then
                        '    '    Await CommWriter(3).StoreAsync()
                        '    'End If
                        'Catch ex As Exception
                        '    Debug.WriteLine("UART TX ERROR " & i.ToString)
                        '    Debug.WriteLine("UART TX ERROR (A)" & ex.Message)
                        '    Debug.WriteLine("UART TX ERROR (B)" & ex.Source)
                        '    ''LCDText("Require REBOOT!", 1)
                        '    Select Case i
                        '        Case 0 : Log.Data.Drive0_ON = 0
                        '        Case 1 : Log.Data.Drive1_ON = 0
                        '    End Select
                        '    UseComm(i) = False

                        '    UARTDevice(i).Dispose()
                        '    CommWriter(i).Dispose()
                        '    'StreamLock(i) = False

                        '    'For j As Integer = 0 To ODrive.Map_OUT.Length - 1
                        '    '    If ODrive.Map_DRV(i) >= 0 Then
                        '    'Next
                        'End Try
                    End If
                Next
            End If

            'RateCounter += 1
            'If RateCounter > TelemetryRate Then
            '    RateCounter = 0
            '    'Debug.WriteLine(Telemetry_Request_ID)
            '    Telemetry_Request_ID += 1
            '    If Telemetry_Request_ID > 15 Then Telemetry_Request_ID = 0
            'End If
            If Not TransmitEvent Then Task.Delay(1).Wait()
            'Task.Delay(1).Wait()
            'Await Task.Delay(5)
        Loop
    End Sub

    Private Async Sub UARTReceiver(ByVal CommID As Integer)

        Dim DataLenght As Integer
        'Dim LocalRX As String = ""
        Dim XChr(256) As Byte
        Dim XValue As Single = 0.0
        'Dim CharRX As Char = ""
        Dim CHValue As Byte = 0
        Dim RegID As Byte = 0
        Dim LocalREG(100) As Single
        Dim REGValue As Single = 0.0
        Dim REGFraction As Single = 0.0
        Dim FracPoint As Single = 0.0
        Dim REGMinus As Boolean = False
        Dim REGDataPresent As Boolean = False
        'Dim REGFound As Boolean = False
        Dim SIDX As Integer = 0
        Dim ReturnValue_Float As Single = 0.0
        Dim ReturnValue_Int As Single = 0
        Dim FilterLenght As Integer = 0
        Dim FilterString As String = ""
        'Dim SValue() As String
        Dim NCount As Integer = 0
        Dim LoopIDX As Integer = 0
        Dim ConvIDX As Integer = 0
        Dim RegIDX As Integer = 0
        Dim XString As New System.Text.StringBuilder
        Const TelemetryCMDCount As Integer = 7
        Dim HeaderOK As Boolean = False
        Dim DataType As Integer = 0
        Dim BRSize(10) As Integer

        SysEvent("Start - UART RX TASK" & CommID.ToString)
        System.Array.Clear(LocalREG, 0, LocalREG.Length)
        BRSize(0) = 256
        BRSize(1) = 256
        BRSize(2) = 256
        BRSize(3) = 256
        BRSize(4) = 256
        BRSize(5) = 256
        BRSize(6) = 256
        BRSize(7) = 256
        BRSize(8) = 256
        BRSize(9) = 256
        BRSize(Reg.Device_Wheel) = 11



        Do Until TaskCancle
            If UseComm(CommID) Then

                DataLenght = Await CommReader(CommID).LoadAsync(BRSize(CommID))
                'CommReader(CommID).ReadBytes(XChr)
                'If DataLenght > 1 Then
                ' ReDim XChr(DataLenght - 1)
                'LocalRX = System.Text.Encoding.ASCII.GetString(XChr, 0, DataLenght)
                'End If

                'Try
                '    DataLenght = Await CommReader(CommID).LoadAsync(256)
                '    If DataLenght > 1 Then
                '        ReDim XChr(DataLenght - 1)
                '        CommReader(CommID).ReadBytes(XChr)
                '        'LocalRX = CommReader(CommID).ReadString(DataLenght)
                '        LocalRX = System.Text.Encoding.ASCII.GetString(XChr, 0, DataLenght)
                '    End If
                'Catch ex As Exception
                '    Debug.WriteLine("UART RX(" & CommID.ToString & ") ERROR " + ex.Message)
                '    UseComm(CommID) = False
                '    Select Case CommID
                '        Case 0 : Log.Data.Drive0_ON = 0
                '        Case 1 : Log.Data.Drive1_ON = 0
                '    End Select
                '    UARTDevice(CommID).Dispose()
                '    CommReader(CommID).Dispose()
                'End Try

                'If DebugEnable Then Debug.WriteLine("UART RX(" & CommID.ToString & ") " & LocalRX)

                'If LocalRX <> "" And DataLenght > 1 Then
                If DataLenght > 1 Then
                    'System.Array.Clear(XChr, 0, XChr.Length)
                    Try
                        For ConvIDX = 0 To DataLenght - 1
                            XChr(ConvIDX) = CommReader(CommID).ReadByte
                        Next
                    Catch ex As Exception
                        DataLenght = 1
                        XChr(0) = " "
                    End Try

                    LocalREG(0) = 0.0
                    LocalREG(1) = 0.0
                    FracPoint = 0.0
                    RegIDX = 0
                    REGValue = 0.0
                    REGFraction = 0.0
                    REGMinus = False
                    NCount = 0

                    ' -- numerical conversion -- for ODrive 
                    If CommID < 4 Then
                        For LoopIDX = 0 To DataLenght - 1
                            CHValue = XChr(LoopIDX)
                            Select Case CHValue
                                Case 46 'Decimal point found
                                    FracPoint = 1.0
                                    REGFraction = 0.0
                                Case 45 'Minus symbol found
                                    REGMinus = True
                                Case 48 To 57 'Number handing
                                    NCount += 1
                                    If FracPoint < 1.0 Then
                                        REGValue = (REGValue * 10.0) + (CHValue - 48.0)
                                    Else
                                        If FracPoint < 100000.0 Then
                                            REGFraction = (REGFraction * 10.0) + (CHValue - 48.0)
                                            FracPoint = FracPoint * 10.0
                                        End If
                                    End If
                                Case Else 'End of numerical value
                                    If NCount > 0 Then
                                        If FracPoint > 0 Then
                                            LocalREG(RegIDX) = REGValue + (REGFraction / FracPoint)
                                        Else
                                            LocalREG(RegIDX) = REGValue
                                        End If
                                        If REGMinus Then
                                            LocalREG(RegIDX) = LocalREG(RegIDX) * -1.0
                                        End If
                                        'If CHValue <> 32 Or RegIDX > 0 Then ' if multiple value found step to next register
                                        'Exit For
                                        'Else
                                        IORegister(CommID, Reg.IO_HEAD + RegIDX) = LocalREG(RegIDX)
                                        FracPoint = 0.0
                                        REGValue = 0.0
                                        REGFraction = 0.0
                                        REGMinus = False
                                        RegIDX += 1
                                        If Reg.IO_HEAD + RegIDX > Reg.IO_Z Then Exit For
                                        'End If

                                    End If
                            End Select
                        Next
                    End If

                    ' -- numerical conversion -- for System PCB , Nextion Display
                    If CommID = Reg.Device_SystemIO Or CommID = Reg.Device_Display Then
                        'If LocalRX.Length > 0 Then
                        '    LocalRX = "@" & LocalRX
                        'End If
                        XString.Clear()
                        RegID = 64 ' @ symbole
                        REGMinus = False
                        REGFraction = 0.0
                        REGValue = 0.0
                        FracPoint = 0.0
                        LocalREG(0) = 0.0
                        REGDataPresent = False
                        'Debug.WriteLine("IORES - " & System.Text.Encoding.ASCII.GetString(XChr, 0, DataLenght))
                        For LoopIDX = 0 To DataLenght - 1
                            CHValue = XChr(LoopIDX)
                            Select Case CHValue
                                Case 45 'Minus symbol found
                                    REGMinus = True
                                    REGDataPresent += 1
                                Case 46 'Decimal point found
                                    FracPoint = 1.0
                                    REGFraction = 0.0
                                Case 48 To 57 'Number handing
                                    REGDataPresent = True
                                    If FracPoint < 1.0 Then
                                        REGValue = (REGValue * 10.0) + (CHValue - 48.0)
                                    Else
                                        If FracPoint < 10000.0 Then
                                            REGFraction = (REGFraction * 10.0) + (CHValue - 48.0)
                                            FracPoint = FracPoint * 10.0
                                        End If
                                    End If
                                Case 64 To 90 ' Register indicator found (this indicate begining of new data set) -> store current value in to last access register
                                    If REGDataPresent Then
                                        If FracPoint > 0 Then
                                            LocalREG(0) = REGValue + (REGFraction / FracPoint)
                                        Else
                                            LocalREG(0) = REGValue
                                        End If
                                        If REGMinus Then
                                            LocalREG(0) = LocalREG(0) * -1.0
                                        End If
                                    End If
                                    IORegister(CommID, RegID) = LocalREG(0)
                                    RegID = CHValue
                                    REGMinus = False
                                    REGFraction = 0.0
                                    REGValue = 0.0
                                    FracPoint = 0.0
                                    LocalREG(0) = 0.0
                                    REGDataPresent = False
                            End Select
                            Select Case CHValue
                                Case 10, 13
                                    XString.Append(Convert.ToChar(CHValue))
                                Case 32 To 126
                                    XString.Append(Convert.ToChar(CHValue))
                            End Select
                        Next
                        If REGDataPresent Then
                            If FracPoint > 0 Then
                                LocalREG(0) = REGValue + (REGFraction / FracPoint)
                            Else
                                LocalREG(0) = REGValue
                            End If
                            If REGMinus Then
                                LocalREG(0) = LocalREG(0) * -1.0
                            End If
                            IORegister(CommID, RegID) = LocalREG(0)
                            RegID = 0
                            REGMinus = False
                            REGFraction = 0.0
                            REGValue = 0.0
                            FracPoint = 0.0
                            LocalREG(0) = 0.0
                            REGDataPresent = False
                        End If
                        If CommID = Reg.Device_SystemIO Then Log.Data.LoadCell = Math.Truncate((Loadcell_Offset - IORegister(Reg.Device_SystemIO, Reg.IO_U)) * Loadcell_Scale).ToString

                    End If

                    ' -- numerical conversion -- for Raspberry PI FFB controller
                    'If CommID = Reg.Device_Wheel Then
                    '    If DataLenght > 2 Then
                    '        'Debug.Write(".")
                    '        If XChr(0) = 87 And XChr(1) = 72 Or True Then
                    '            Debug.Write(".")
                    '            Select Case XChr(2)
                    '                Case 0
                    '                    Wheel.WheelPOS = (XChr(3) * 256) + XChr(4)
                    '                    Wheel.OprState = 0
                    '                    Wheel.AnalogCH_1 = XChr(5)
                    '                    Wheel.AnalogCH_2 = XChr(6)
                    '                    Wheel.AnalogCH_3 = XChr(7)
                    '                    Wheel.AnalogCH_4 = XChr(8)
                    '                    Wheel.Button = (XChr(9) * 256) + XChr(10)
                    '                    'If Not Wheel_Shadow.Equals(Wheel) Then
                    '                    '    Wheel_Shadow = Wheel
                    '                    '    WriteRespond("", RespondTo.PC)
                    '                    'End If
                    '                Case 1
                    '                    Wheel.OprState = 1
                    '                    DriveError(Reg.Device_Wheel).General = (XChr(3) * 256) + XChr(4)
                    '                    DriveError(Reg.Device_Wheel).Motor = (XChr(5) * 256) + XChr(6)
                    '                    DriveError(Reg.Device_Wheel).Encoder = (XChr(7) * 256) + XChr(8)
                    '                    Wheel.Button = (XChr(9) * 256) + XChr(10)
                    '                Case Else
                    '                    Debug.Write(" " & XChr(0).ToString)
                    '            End Select
                    '        End If
                    '    End If
                    'End If



                    'FilterString = LocalRX.Replace(vbCr, " ")
                    'FilterString = FilterString.Replace(vbLf, " ")
                    'FilterLenght = FilterString.Length

                    'Select Case CommID
                    '    Case 0
                    '        Log.Data.Drive0RX = FilterString
                    '        Exit Select
                    '    Case 1
                    '        Log.Data.Drive1RX = FilterString
                    '        Exit Select
                    '    Case 2
                    '        Log.Data.BUSRX = FilterString
                    '        Exit Select
                    '    Case 3
                    '        Log.Data.Display1RX = FilterString
                    '        Exit Select
                    'End Select

                    'If LocalRX > 2 Then
                    '    Select Case CommID
                    '        Case = 0
                    '            Log.Data.Drive0RX = xData.Substring(0, xLenght - 2)
                    '        Case = 1
                    '            Log.Data.Drive1TX = xData.Substring(0, xLenght - 2)
                    '        Case = 2
                    '            Log.Data.BUSTX = xData
                    '        Case = 3
                    '            Log.Data.Display1TX = xData.Substring(0, xLenght - 3)
                    '    End Select
                    '    Log.Add()
                    'End If

                    If TelemetryEnable And CommID < 4 Then

                        'Select Case CommID
                        '    Case 0
                        '        Select Case TelemetryIndex(0)
                        '            Case 0, 1, 3, 4
                        '                SValue = FilterString.Split(" ")

                        '                If Single.TryParse(SValue(0), ReturnValue_Float) Then Log.Data.Axis0_APOS = Math.Round((ReturnValue_Float - ControlRegister(Register.Axis0_Offset)) / Axis0_PPD, 2)
                        '                If SValue.Length > 1 Then
                        '                    If Single.TryParse(SValue(1), ReturnValue_Float) Then Log.Data.Axis0_RPM = Math.Truncate((ReturnValue_Float / Axis0_PPR) * 60).ToString
                        '                End If

                        '                'SIDX = FilterString.IndexOf(" ")
                        '                'If SIDX > 0 And SIDX < FilterLenght Then
                        '                '    If Single.TryParse(FilterString.Substring(0, SIDX), ReturnValue_Float) Then
                        '                '        Log.Data.Axis0_APOS = Math.Round((ReturnValue_Float - ControlRegister(Reg.Axis0_Offset)) / Axis0_PPD, 2)
                        '                '    End If
                        '                '    If Single.TryParse(FilterString.Substring(SIDX + 0), ReturnValue_Float) Then
                        '                '        Log.Data.Axis0_RPM = Math.Truncate((ReturnValue_Float / Axis0_PPR) * 60).ToString
                        '                '    End If
                        '                '    'Log.Data.Axis0_APOS = Math.Round((AxisMath.toSingle(FilterString.Substring(0, SIDX)) - ControlRegister(Reg.Axis0_Offset)) / Axis0_PPD, 2)
                        '                '    'Log.Data.Axis0_RPM = Math.Truncate((AxisMath.toSingle(FilterString.Substring(SIDX + 0)) / Axis2_PPR) * 60).ToString
                        '                'End If
                        '                Exit Select
                        '            Case 2
                        '                If Single.TryParse(FilterString, ReturnValue_Float) Then
                        '                    Log.Data.Axis0_CUR = Math.Round(ReturnValue_Float, 2).ToString
                        '                End If
                        '                'Log.Data.Axis0_CUR = Math.Round(AxisMath.toSingle(FilterString), 2).ToString
                        '                Exit Select
                        '            Case 5
                        '                'If Integer.TryParse(FilterString, ReturnValue_Int) Then
                        '                '    Log.Data.Axis0_ERR_EXT = ReturnValue_Int.ToString
                        '                'End If
                        '                'Log.Data.Axis0_ERR_EXT = AxisMath.toSingle(FilterString).ToString
                        '                Log.Data.Axis0_ERR_EXT = FilterString
                        '                Exit Select
                        '            Case 6, 7, 9, 10
                        '                SValue = FilterString.Split(" ")
                        '                If Single.TryParse(SValue(0), ReturnValue_Float) Then Log.Data.Axis1_APOS = Math.Round((ReturnValue_Float - ControlRegister(Register.Axis1_Offset)) / Axis1_PPD, 2)
                        '                If SValue.Length > 1 Then
                        '                    If Single.TryParse(SValue(1), ReturnValue_Float) Then Log.Data.Axis1_RPM = Math.Truncate((ReturnValue_Float / Axis1_PPR) * 60).ToString
                        '                End If

                        '                'SIDX = FilterString.IndexOf(" ")
                        '                'If SIDX > 0 And SIDX < FilterLenght Then
                        '                '    If Single.TryParse(FilterString.Substring(0, SIDX), ReturnValue_Float) Then
                        '                '        Log.Data.Axis1_APOS = Math.Round((ReturnValue_Float - ControlRegister(Reg.Axis1_Offset)) / Axis1_PPD, 2)
                        '                '    End If
                        '                '    'If Single.TryParse(FilterString.Substring(SIDX + 0), ReturnValue_Float) Then
                        '                '    '    Log.Data.Axis1_RPM = Math.Truncate((ReturnValue_Float / Axis1_PPR) * 60).ToString
                        '                '    'End If
                        '                '    'Log.Data.Axis1_APOS = Math.Round((AxisMath.toSingle(FilterString.Substring(0, SIDX)) - ControlRegister(Reg.Axis1_Offset)) / Axis1_PPD, 2)
                        '                '    'Log.Data.Axis1_RPM = Math.Truncate((AxisMath.toSingle(FilterString.Substring(SIDX + 0)) / Axis2_PPR) * 60).ToString
                        '                'End If
                        '                Exit Select
                        '            Case 8
                        '                If Single.TryParse(FilterString, ReturnValue_Float) Then
                        '                    Log.Data.Axis1_CUR = Math.Round(ReturnValue_Float, 2).ToString
                        '                End If
                        '                'Log.Data.Axis1_CUR = Math.Round(AxisMath.toSingle(FilterString), 2).ToString
                        '                Exit Select
                        '            Case 11
                        '                'If Integer.TryParse(FilterString, ReturnValue_Int) Then
                        '                '    Log.Data.Axis1_ERR_EXT = ReturnValue_Int.ToString
                        '                'End If
                        '                'Log.Data.Axis1_ERR_EXT = AxisMath.toSingle(FilterString).ToString
                        '                Log.Data.Axis1_ERR_EXT = FilterString
                        '                Exit Select
                        '        End Select
                        '        TelemetryCounter(0) = 0
                        '        TelemetryIndex(0) += 1
                        '        If ODrive.Motor(0).Enable = False And TelemetryIndex(0) < 6 Then TelemetryIndex(0) = 6
                        '        If ODrive.Motor(1).Enable = False And TelemetryIndex(0) > 5 Then TelemetryIndex(0) = 0
                        '        If TelemetryIndex(0) > 11 Then TelemetryIndex(0) = 0
                        '        Exit Select
                        '    Case 1
                        '        Select Case TelemetryIndex(1)
                        '            Case 0, 1, 3, 4
                        '                SValue = FilterString.Split(" ")
                        '                If Single.TryParse(SValue(0), ReturnValue_Float) Then Log.Data.Axis2_APOS = Math.Round((ReturnValue_Float - ControlRegister(Register.Axis2_Offset)) / Axis2_PPD, 2)
                        '                If SValue.Length > 1 Then
                        '                    If Single.TryParse(SValue(1), ReturnValue_Float) Then Log.Data.Axis2_RPM = Math.Truncate((ReturnValue_Float / Axis2_PPR) * 60).ToString
                        '                End If

                        '                'SIDX = FilterString.IndexOf(" ")
                        '                'If SIDX > 0 And SIDX < FilterLenght Then
                        '                '    If Single.TryParse(FilterString.Substring(0, SIDX), ReturnValue_Float) Then
                        '                '        Log.Data.Axis2_APOS = Math.Round((ReturnValue_Float - ControlRegister(Reg.Axis2_Offset)) / Axis2_PPD, 2)
                        '                '    End If
                        '                '    'If Single.TryParse(FilterString.Substring(SIDX + 0), ReturnValue_Float) Then
                        '                '    '    Log.Data.Axis2_RPM = Math.Truncate((ReturnValue_Float / Axis2_PPR) * 60).ToString
                        '                '    'End If
                        '                '    'Log.Data.Axis2_APOS = Math.Round((AxisMath.toSingle(FilterString.Substring(0, SIDX)) - ControlRegister(Reg.Axis2_Offset)) / Axis2_PPD, 2)
                        '                '    'Log.Data.Axis2_RPM = Math.Truncate((AxisMath.toSingle(FilterString.Substring(SIDX + 0)) / Axis2_PPR) * 60).ToString
                        '                'End If
                        '                Exit Select
                        '            Case 2
                        '                If Single.TryParse(FilterString, ReturnValue_Float) Then
                        '                    Log.Data.Axis2_CUR = Math.Round(ReturnValue_Float, 2).ToString
                        '                End If
                        '                'Log.Data.Axis2_CUR = Math.Round(AxisMath.toSingle(FilterString), 2).ToString
                        '                Exit Select
                        '            Case 5
                        '                'If Integer.TryParse(FilterString, ReturnValue_Int) Then
                        '                '    Log.Data.Axis2_ERR_EXT = ReturnValue_Int.ToString
                        '                'End If
                        '                'Log.Data.Axis2_ERR_EXT = AxisMath.toSingle(FilterString).ToString
                        '                Log.Data.Axis2_ERR_EXT = FilterString
                        '                Exit Select
                        '            Case 6, 7, 9, 10
                        '                SValue = FilterString.Split(" ")
                        '                If Single.TryParse(SValue(0), ReturnValue_Float) Then Log.Data.Axis3_APOS = Math.Round((ReturnValue_Float - ControlRegister(Register.Axis3_Offset)) / Axis3_PPD, 2)
                        '                If SValue.Length > 1 Then
                        '                    If Single.TryParse(SValue(1), ReturnValue_Float) Then Log.Data.Axis3_RPM = Math.Truncate((ReturnValue_Float / Axis3_PPR) * 60).ToString
                        '                End If

                        '                'SIDX = FilterString.IndexOf(" ")
                        '                'If SIDX > 0 And SIDX < FilterLenght Then
                        '                '    If Single.TryParse(FilterString.Substring(0, SIDX), ReturnValue_Float) Then
                        '                '        Log.Data.Axis3_APOS = Math.Round((ReturnValue_Float - ControlRegister(Reg.Axis3_Offset)) / Axis3_PPD, 2)
                        '                '    End If
                        '                '    'If Single.TryParse(FilterString.Substring(SIDX + 0), ReturnValue_Float) Then
                        '                '    '    Log.Data.Axis3_RPM = Math.Truncate((ReturnValue_Float / Axis3_PPR) * 60).ToString
                        '                '    'End If
                        '                '    'Log.Data.Axis3_APOS = Math.Round((AxisMath.toSingle(FilterString.Substring(0, SIDX)) - ControlRegister(Reg.Axis3_Offset)) / Axis3_PPD, 2)
                        '                '    'Log.Data.Axis3_RPM = Math.Truncate((AxisMath.toSingle(FilterString.Substring(SIDX + 0)) / Axis2_PPR) * 60).ToString
                        '                'End If
                        '                Exit Select
                        '            Case 8
                        '                If Single.TryParse(FilterString, ReturnValue_Float) Then
                        '                    Log.Data.Axis3_CUR = Math.Round(ReturnValue_Float, 2).ToString
                        '                End If
                        '                'Log.Data.Axis3_CUR = Math.Round(AxisMath.toSingle(FilterString), 2).ToString
                        '                Exit Select
                        '            Case 11
                        '                'If Integer.TryParse(FilterString, ReturnValue_Int) Then
                        '                '     Log.Data.Axis3_ERR_EXT = ReturnValue_Int.ToString
                        '                'End If
                        '                'Log.Data.Axis3_ERR_EXT = AxisMath.toSingle(FilterString).ToString
                        '                Log.Data.Axis3_ERR_EXT = FilterString
                        '                Exit Select
                        '        End Select
                        '        TelemetryCounter(1) = 0
                        '        TelemetryIndex(1) += 1
                        '        If ODrive.Motor(2).Enable = False And TelemetryIndex(1) < 6 Then TelemetryIndex(1) = 6
                        '        If ODrive.Motor(3).Enable = False And TelemetryIndex(1) > 5 Then TelemetryIndex(1) = 0
                        '        If TelemetryIndex(1) > 11 Then TelemetryIndex(1) = 0
                        '        Exit Select

                        'End Select
                        'Log.Data.Axis0_APOS = FilterString


                        Select Case Telemetry_Request_ID(CommID)
                            Case TelemitryCMD_ID.ACTUAL_POSITION_M0
                                Select Case CommID
                                    Case 0 : Log.Data.Axis_APOS(0) = LocalREG(0) : If RegIDX > 0 Then Log.Data.Axis_RPM(0) = LocalREG(1) * 60.0
                                    Case 1 : Log.Data.Axis_APOS(2) = LocalREG(0) : If RegIDX > 0 Then Log.Data.Axis_RPM(2) = LocalREG(1) * 60.0
                                    Case 2 : Log.Data.Axis_APOS(4) = LocalREG(0) : If RegIDX > 0 Then Log.Data.Axis_RPM(4) = LocalREG(1) * 60.0
                                End Select
                            Case TelemitryCMD_ID.MOTOR_CURRENT_M0
                                Select Case CommID
                                    Case 0 : Log.Data.Axis_CUR(0) = LocalREG(0)
                                    Case 1 : Log.Data.Axis_CUR(2) = LocalREG(0)
                                    Case 2 : Log.Data.Axis_CUR(4) = LocalREG(0)
                                End Select
                            Case TelemitryCMD_ID.ACTUAL_POSITION_M1
                                Select Case CommID
                                    Case 0 : Log.Data.Axis_APOS(1) = LocalREG(0) : If RegIDX > 0 Then Log.Data.Axis_RPM(1) = LocalREG(1) * 60.0
                                    Case 1 : Log.Data.Axis_APOS(3) = LocalREG(0) : If RegIDX > 0 Then Log.Data.Axis_RPM(3) = LocalREG(1) * 60.0
                                    Case 2 : Log.Data.Axis_APOS(5) = LocalREG(0) : If RegIDX > 0 Then Log.Data.Axis_RPM(5) = LocalREG(1) * 60.0
                                End Select
                            Case TelemitryCMD_ID.MOTOR_CURRENT_M1
                                Select Case CommID
                                    Case 0 : Log.Data.Axis_CUR(1) = LocalREG(0)
                                    Case 1 : Log.Data.Axis_CUR(3) = LocalREG(0)
                                    Case 2 : Log.Data.Axis_CUR(5) = LocalREG(0)
                                End Select
                            Case TelemitryCMD_ID.TEMPERATURE_M0
                                Select Case CommID
                                    Case 0 : Log.Data.Axis_TEMP(0) = LocalREG(0)
                                    Case 1 : Log.Data.Axis_TEMP(2) = LocalREG(0)
                                    Case 2 : Log.Data.Axis_TEMP(4) = LocalREG(0)
                                End Select
                            Case TelemitryCMD_ID.TEMPERATURE_M1
                                Select Case CommID
                                    Case 0 : Log.Data.Axis_TEMP(1) = LocalREG(0)
                                    Case 1 : Log.Data.Axis_TEMP(3) = LocalREG(0)
                                    Case 2 : Log.Data.Axis_TEMP(5) = LocalREG(0)
                                End Select
                        End Select
                        Telemetry_Request_ID(CommID) += 1
                        If Telemetry_Request_ID(CommID) > TelemetryCMDCount Then Telemetry_Request_ID(CommID) = 0

                        'Select Case Telemetry_Request_ID
                        '    Case 0
                        '        'SValue = FilterString.Split(" ")
                        '        'If SValue.Length > 1 Then
                        '        '    'If Single.TryParse(SValue(0), ReturnValue_Float) Then Log.Data.Axis0_APOS = Math.Round((ReturnValue_Float - ControlRegister(Register.Axis0_Offset)) / Axis0_PPD, 2)
                        '        '    'If Single.TryParse(SValue(1), ReturnValue_Float) Then Log.Data.Axis0_RPM = Math.Truncate((ReturnValue_Float / Axis0_PPR) * 60).ToString
                        '        '    Log.Data.Axis0_APOS = SValue(0)
                        '        '    Log.Data.Axis0_RPM = SValue(1)
                        '        'Else
                        '        '    Debug.WriteLine(Telemetry_Request_ID.ToString & ":" & FilterString)
                        '        'End If
                        '        Log.Data.Axis0_APOS = LocalREG(0).ToString
                        '        If RegIDX > 0 Then Log.Data.Axis0_RPM = LocalREG(1).ToString
                        '    Case 1
                        '        'SValue = FilterString.Split(" ")
                        '        'If SValue.Length > 1 Then
                        '        '    'If Single.TryParse(SValue(0), ReturnValue_Float) Then Log.Data.Axis1_APOS = Math.Round((ReturnValue_Float - ControlRegister(Register.Axis1_Offset)) / Axis1_PPD, 2)
                        '        '    'If Single.TryParse(SValue(1), ReturnValue_Float) Then Log.Data.Axis1_RPM = Math.Truncate((ReturnValue_Float / Axis1_PPR) * 60).ToString
                        '        '    Log.Data.Axis1_APOS = SValue(0)
                        '        '    Log.Data.Axis1_RPM = SValue(1)
                        '        'Else
                        '        '    Debug.WriteLine(Telemetry_Request_ID.ToString & ":" & FilterString)
                        '        'End If
                        '        Log.Data.Axis1_APOS = LocalREG(0).ToString
                        '        If RegIDX > 0 Then Log.Data.Axis1_RPM = LocalREG(1).ToString
                        '    Case 2
                        '        'SValue = FilterString.Split(" ")
                        '        'If SValue.Length > 1 Then
                        '        '    'If Single.TryParse(SValue(0), ReturnValue_Float) Then Log.Data.Axis2_APOS = Math.Round((ReturnValue_Float - ControlRegister(Register.Axis2_Offset)) / Axis2_PPD, 2)
                        '        '    'If Single.TryParse(SValue(1), ReturnValue_Float) Then Log.Data.Axis2_RPM = Math.Truncate((ReturnValue_Float / Axis2_PPR) * 60).ToString
                        '        '    Log.Data.Axis2_APOS = SValue(0)
                        '        '    Log.Data.Axis2_RPM = SValue(1)
                        '        'Else
                        '        '    Debug.WriteLine(Telemetry_Request_ID.ToString & ":" & FilterString)
                        '        'End If
                        '        Log.Data.Axis2_APOS = LocalREG(0).ToString
                        '        If RegIDX > 0 Then Log.Data.Axis2_RPM = LocalREG(1).ToString
                        '    Case 3
                        '        'SValue = FilterString.Split(" ")
                        '        'If SValue.Length > 1 Then
                        '        '    'If Single.TryParse(SValue(0), ReturnValue_Float) Then Log.Data.Axis3_APOS = Math.Round((ReturnValue_Float - ControlRegister(Register.Axis3_Offset)) / Axis3_PPD, 2)
                        '        '    'If Single.TryParse(SValue(1), ReturnValue_Float) Then Log.Data.Axis3_RPM = Math.Truncate((ReturnValue_Float / Axis3_PPR) * 60).ToString
                        '        '    Log.Data.Axis3_APOS = SValue(0)
                        '        '    Log.Data.Axis3_RPM = SValue(1)
                        '        'Else
                        '        '    Debug.WriteLine(Telemetry_Request_ID.ToString & ":" & FilterString)
                        '        'End If
                        '        Log.Data.Axis3_APOS = LocalREG(0).ToString
                        '        If RegIDX > 0 Then Log.Data.Axis3_RPM = LocalREG(1).ToString
                        '    Case 4
                        '        'SValue = FilterString.Split(" ")
                        '        'If SValue.Length > 1 Then
                        '        '    'If Single.TryParse(SValue(0), ReturnValue_Float) Then Log.Data.Axis4_APOS = Math.Round((ReturnValue_Float - ControlRegister(Register.Axis4_Offset)) / Axis0_PPD, 2)
                        '        '    'If Single.TryParse(SValue(1), ReturnValue_Float) Then Log.Data.Axis4_RPM = Math.Truncate((ReturnValue_Float / Axis0_PPR) * 60).ToString
                        '        '    Log.Data.Axis4_APOS = SValue(0)
                        '        '    Log.Data.Axis4_RPM = SValue(1)
                        '        'Else
                        '        '    Debug.WriteLine(Telemetry_Request_ID.ToString & ":" & FilterString)
                        '        'End If
                        '        Log.Data.Axis4_APOS = LocalREG(0).ToString
                        '        If RegIDX > 0 Then Log.Data.Axis4_RPM = LocalREG(1).ToString
                        '    Case 5
                        '        'SValue = FilterString.Split(" ")
                        '        'If SValue.Length > 1 Then
                        '        '    'If Single.TryParse(SValue(0), ReturnValue_Float) Then Log.Data.Axis5_APOS = Math.Round((ReturnValue_Float - ControlRegister(Register.Axis5_Offset)) / Axis0_PPD, 2)
                        '        '    'If Single.TryParse(SValue(1), ReturnValue_Float) Then Log.Data.Axis5_RPM = Math.Truncate((ReturnValue_Float / Axis0_PPR) * 60).ToString
                        '        '    Log.Data.Axis5_APOS = SValue(0)
                        '        '    Log.Data.Axis5_RPM = SValue(1)
                        '        'Else
                        '        '    Debug.WriteLine(Telemetry_Request_ID.ToString & ":" & FilterString)
                        '        'End If
                        '        Log.Data.Axis5_APOS = LocalREG(0).ToString
                        '        If RegIDX > 0 Then Log.Data.Axis5_RPM = LocalREG(1).ToString
                        '    Case 6 '--- ? ---
                        '    Case 7 '--- ? ---
                        '    Case 8 : Log.Data.Axis0_CUR = LocalREG(0).ToString 'If Single.TryParse(FilterString, ReturnValue_Float) Then Log.Data.Axis0_CUR = Math.Round(ReturnValue_Float, 2).ToString
                        '    Case 9 : Log.Data.Axis1_CUR = LocalREG(0).ToString 'If Single.TryParse(FilterString, ReturnValue_Float) Then Log.Data.Axis1_CUR = Math.Round(ReturnValue_Float, 2).ToString
                        '    Case 10 : Log.Data.Axis2_CUR = LocalREG(0).ToString 'If Single.TryParse(FilterString, ReturnValue_Float) Then Log.Data.Axis2_CUR = Math.Round(ReturnValue_Float, 2).ToString
                        '    Case 11 : Log.Data.Axis3_CUR = LocalREG(0).ToString 'If Single.TryParse(FilterString, ReturnValue_Float) Then Log.Data.Axis3_CUR = Math.Round(ReturnValue_Float, 2).ToString
                        '    Case 12 : Log.Data.Axis4_CUR = LocalREG(0).ToString 'If Single.TryParse(FilterString, ReturnValue_Float) Then Log.Data.Axis4_CUR = Math.Round(ReturnValue_Float, 2).ToString
                        '    Case 13 : Log.Data.Axis5_CUR = LocalREG(0).ToString 'If Single.TryParse(FilterString, ReturnValue_Float) Then Log.Data.Axis5_CUR = Math.Round(ReturnValue_Float, 2).ToString
                        '    Case 14  '--- ? ---
                        '    Case 15  '--- ? ---
                        '    Case 16 : Log.Data.Axis0_ERR_EXT = LocalREG(0).ToString 'If Integer.TryParse(FilterString, ReturnValue_Int) Then Log.Data.Axis0_ERR_EXT = ReturnValue_Int.ToString
                        '    Case 17 : Log.Data.Axis0_ERR_EXT = LocalREG(0).ToString 'If Integer.TryParse(FilterString, ReturnValue_Int) Then Log.Data.Axis1_ERR_EXT = ReturnValue_Int.ToString
                        '    Case 18 : Log.Data.Axis0_ERR_EXT = LocalREG(0).ToString 'If Integer.TryParse(FilterString, ReturnValue_Int) Then Log.Data.Axis2_ERR_EXT = ReturnValue_Int.ToString
                        '    Case 19 : Log.Data.Axis0_ERR_EXT = LocalREG(0).ToString 'If Integer.TryParse(FilterString, ReturnValue_Int) Then Log.Data.Axis3_ERR_EXT = ReturnValue_Int.ToString
                        '    Case 20 : Log.Data.Axis0_ERR_EXT = LocalREG(0).ToString 'If Integer.TryParse(FilterString, ReturnValue_Int) Then Log.Data.Axis4_ERR_EXT = ReturnValue_Int.ToString
                        '    Case 21 : Log.Data.Axis0_ERR_EXT = LocalREG(0).ToString 'If Integer.TryParse(FilterString, ReturnValue_Int) Then Log.Data.Axis5_ERR_EXT = ReturnValue_Int.ToString
                        '    Case 22 '--- ? ---
                        '    Case 23 '--- ? ---
                        'End Select
                        'Telemetry_Request_ID += 1
                        'If Telemetry_Request_ID > 15 Then Telemetry_Request_ID = 0
                    Else
                        XString.Clear()
                        For LoopIDX = 0 To DataLenght - 1
                            CHValue = XChr(LoopIDX)
                            Select Case CHValue
                                Case 10, 13
                                    XString.Append(Convert.ToChar(CHValue))
                                Case 32 To 126
                                    XString.Append(Convert.ToChar(CHValue))
                            End Select
                        Next
                    End If
                    Log.Add()
                End If

                'If StreamLock(CommID) Then
                'StreamLock(CommID) = False
                'If ControlRegister(Reg.Game_Controller_Type) = 0 And ODriveWheel_ID = CommID Then
                'TelemetryDataS(9) = LocalRX
                'Else
                'If CommID = 1 Then
                'If CommID = 0 Then
                '    Select Case TelemetryIndex
                '        Case = 0
                '            'TelemetryDataS(Telemetry_Slot_Select, Telemetry_POS_RPM_Actual) = LocalRX
                '            SIDX = FilterString.IndexOf(" ")
                '            If SIDX > 0 And SIDX < FilterLenght Then
                '                Log.Data.Axis0_APOS = FilterString.Substring(0, SIDX)
                '                Log.Data.Axis0_RPM = FilterString.Substring(SIDX + 0)
                '            End If
                '        Case = 1
                '            'TelemetryDataS(Telemetry_Slot_Select, Telemetry_CUR_Command) = LocalRX
                '            Log.Data.Axis0_CUR = FilterString
                '        Case = 2
                '            'TelemetryDataS(Telemetry_Slot_Select, Telemetry_CUR_Measured) = LocalRX
                '            SIDX = FilterString.IndexOf(" ")
                '            If SIDX > 0 And SIDX < FilterLenght Then
                '                Log.Data.Axis1_APOS = FilterString.Substring(0, SIDX)
                '                Log.Data.Axis1_RPM = FilterString.Substring(SIDX + 0)
                '            End If
                '        Case = 3
                '            Log.Data.Axis1_CUR = FilterString
                '    End Select
                'End If
                'If CommID = 1 Then
                '    Select Case TelemetryIndex
                '        Case = 0
                '            'TelemetryDataS(Telemetry_Slot_Select, Telemetry_POS_RPM_Actual) = LocalRX
                '            SIDX = FilterString.IndexOf(" ")
                '            If SIDX > 0 And SIDX < FilterLenght Then
                '                Log.Data.Axis2_APOS = FilterString.Substring(0, SIDX)
                '                Log.Data.Axis2_RPM = FilterString.Substring(SIDX + 0)
                '            End If
                '        Case = 1
                '            'TelemetryDataS(Telemetry_Slot_Select, Telemetry_CUR_Command) = LocalRX
                '            Log.Data.Axis2_CUR = FilterString
                '        Case = 2
                '            'TelemetryDataS(Telemetry_Slot_Select, Telemetry_CUR_Measured) = LocalRX
                '            SIDX = FilterString.IndexOf(" ")
                '            If SIDX > 0 And SIDX < FilterLenght Then
                '                Log.Data.Axis3_APOS = FilterString.Substring(0, SIDX)
                '                Log.Data.Axis3_RPM = FilterString.Substring(SIDX + 0)
                '            End If
                '        Case = 3
                '            Log.Data.Axis3_CUR = FilterString
                '    End Select
                'End If

                'TelemetryIndex += 1
                'If TelemetryIndex > 3 Then
                '    TelemetryIndex = 0
                'End If
                'End If
                'End If

                'Else
                If DataLenght > 1 Then
                    CommRX(CommID) = XString.ToString
                    CommRXShadow(CommID) = XString.ToString
                End If
            Else
                Task.Delay(1).Wait()
            End If
        Loop
    End Sub

    Private Async Sub UARTReceiver_Wheel()
        Dim DataLenght As Integer
        'Dim LocalRX As String = ""
        Dim XChr(256) As Byte
        Dim ZChr() As Byte = {5, 5, 5, 5, 5, 5, 5, 5, 5}
        'Dim XValue As Single = 0.0
        'Dim CharRX As Char = ""
        'Dim CHValue As Byte = 0
        'Dim RegID As Byte = 0
        Dim LocalREG(100) As Single
        'Dim REGValue As Single = 0.0
        'Dim REGFraction As Single = 0.0
        'Dim FracPoint As Single = 0.0
        'Dim REGMinus As Boolean = False
        'Dim REGFound As Boolean = False
        'Dim SIDX As Integer = 0
        'Dim ReturnValue_Float As Single = 0.0
        'Dim ReturnValue_Int As Single = 0
        'Dim FilterLenght As Integer = 0
        'Dim FilterString As String = ""
        'Dim SValue() As String
        'Dim NCount As Integer = 0
        'Dim LoopIDX As Integer = 0
        'Dim ConvIDX As Integer = 0
        'Dim RegIDX As Integer = 0
        'Dim XString As New System.Text.StringBuilder
        'Dim HeaderOK As Boolean = False
        'Dim DataType As Integer = 0

        SysEvent("Start - UART RX TASK - Wheel")
        System.Array.Clear(LocalREG, 0, LocalREG.Length)

        Do Until TaskCancle
            If UseComm(Reg.Device_Wheel) Then

                DataLenght = Await CommReader(Reg.Device_Wheel).LoadAsync(11)

                If DataLenght > 1 Then
                    Try
                        For ConvIDX = 0 To DataLenght - 1
                            XChr(ConvIDX) = CommReader(Reg.Device_Wheel).ReadByte
                        Next
                    Catch ex As Exception
                        DataLenght = 1
                        XChr(0) = " "
                    End Try

                    ' -- numerical conversion -- for Raspberry PI FFB controller
                    If DataLenght > 2 Then

                        If XChr(0) = 87 And XChr(1) = 72 Then

                            Select Case XChr(2)
                                Case 99 '0
                                    Wheel.WheelPOS = (XChr(3) * 256) + XChr(4)
                                    Wheel.OprState = 0
                                    Wheel.AnalogCH_1 = XChr(5)
                                    Wheel.AnalogCH_2 = XChr(6)
                                    Wheel.AnalogCH_3 = XChr(7)
                                    Wheel.AnalogCH_4 = XChr(8)
                                    Wheel.Button = (XChr(9) * 256) + XChr(10)
                                        'If Not Wheel_Shadow.Equals(Wheel) Then
                                        '    Wheel_Shadow = Wheel
                                        '    WriteRespond("", RespondTo.PC)
                                        'End If
                                Case 1
                                    Wheel.OprState = 1
                                    DriveError(Reg.Device_Wheel).General = (XChr(3) * 256) + XChr(4)
                                    DriveError(Reg.Device_Wheel).Motor = (XChr(5) * 256) + XChr(6)
                                    DriveError(Reg.Device_Wheel).Encoder = (XChr(7) * 256) + XChr(8)
                                    Wheel.Button = (XChr(9) * 256) + XChr(10)
                                Case Else
                                    Debug.Write(" " & XChr(0).ToString)
                            End Select
                        End If
                    End If
                End If
            Else
                Task.Delay(1).Wait()
            End If
        Loop
    End Sub

    Private Async Sub GlobalUARTResponder()
        Dim UARTRes As New StringBuilder
        Dim DataReady As Boolean = False
        'Dim TransferBuffer() As Byte
        Do Until TaskCancle
            DataReady = False
            For i As Integer = 0 To 3
                If UseComm(i) Then
                    If CommRX(i) <> vbNullString Then
                        If DataReady Then UARTRes.Append(":")
                        UARTRes.Append("[")
                        UARTRes.Append(RecvCMDID.ToString)
                        UARTRes.Append("]DRIVE(")
                        UARTRes.Append(i.ToString)
                        UARTRes.Append(")=")
                        UARTRes.Append(CommRX(i))
                        CommRX(i) = ""
                        DataReady = True
                        'StreamLock(i) = False
                    End If
                End If
            Next
            If DataReady Then
                If Not TelemetryEnable Then
                    'TransferBuffer = Ascii.GetBytes(UARTRes.ToString)
                    'Await NETTransmitter.SendAsync(TransferBuffer, TransferBuffer.Length, PCEndpoint)
                    'Await NETTransmitter.SendAsync(TransferBuffer, TransferBuffer.Length, DashboardEndpoint)
                    WriteRespondBinary(System.Text.Encoding.ASCII.GetBytes(UARTRes.ToString), RespondTo.PC)
                    WriteRespondBinary(System.Text.Encoding.ASCII.GetBytes(UARTRes.ToString), RespondTo.Dashboard)
                End If
                UARTRes.Clear()
            End If
            Task.Delay(10).Wait()
        Loop
    End Sub

    Private Sub StateControl()

        Const SoftStartScale As Single = 0.1
        Const CoinIOSlot As Integer = Reg.IO_S
        Const ServiceIOSlot As Integer = Reg.IO_T
        Const RawIOSlot As Integer = Reg.IO_R
        Const BinaryMode As Boolean = True

        Const stallMargin As Single = 0.1
        Const stallDelayLimit As Integer = 100

        Dim stallDelayCounter(6) As Integer
        Dim stallDistant(6) As Single
        Dim MotorPOSShadow(6) As Single
        Dim CommandPOSShadow(6) As Single

        Dim DebugMSGEventTick As Boolean = False

        Dim WheelRespondByte(11) As Byte
        Dim WheelCommandByte(11) As Byte
        'Dim WheelCommandByte(6) As Byte
        Dim WheelCommandUpdate As Boolean = False
        Dim ExtIORespondByte(99) As Byte
        'Dim TempByteConv As UInt16 = 0
        Dim ExtIOEvent As Boolean = False
        'Dim TRes As New StringBuilder
        Dim DRes As New StringBuilder
        Dim TransferBuffer(256) As Byte
        Dim BTNArray(32) As Boolean
        Dim AxisArray(3) As Double
        Dim SWArray(32) As GameControllerSwitchPosition
        'Dim FFBX As Windows.Gaming.Input.ForceFeedback.ForceFeedbackMotor
        Dim COMB As Integer = 0
        Dim P_Gas As Integer = 0
        Dim P_Brake As Integer = 0
        Dim BTNValue As Integer = 0
        Dim HaveData As Boolean = False
        Dim Run_CNT As Integer = 0
        Dim LocalMotorRunState As Integer = 0
        Dim LocalMotorModifierState As Integer = 0
        'Dim ManualMotorState As Integer = 0
        Dim ReqStateBuffer As Boolean = True 'Enable buffered request command
        Dim CMD_Manual As String = ""
        Dim FTP_KACounter As Integer = 0
        Dim SelectProfile As Integer = 0
        Dim OutputScale As Single = 0.0
        Dim ScaleCount As Integer = 0
        'Const ScaleDelay As Integer = 10
        Dim IO_CNT As Integer = 0
        Dim IO_TEMP As Single = 0
        Dim xStr As New System.Text.StringBuilder
        Dim Converter_Uint16 As UInt16 = 0
        Dim Converter_int16 As Int16 = 0
        Dim Array_16bit(2) As Byte
        Dim xRandom As New Random

        SysEvent("Start - State Machine")
        WheelRespondByte(0) = 120
        WheelRespondByte(1) = 68
        ExtIORespondByte(0) = 120
        ExtIORespondByte(1) = 68
        WheelCommandByte(0) = 87
        WheelCommandByte(1) = 72
        WheelCommandByte(2) = 128

        System.Array.Clear(stallDelayCounter, 0, stallDelayCounter.Length)
        System.Array.Clear(stallDistant, 0, stallDistant.Length)
        System.Array.Clear(MotorPOSShadow, 0, MotorPOSShadow.Length)
        System.Array.Clear(CommandPOSShadow, 0, CommandPOSShadow.Length)

        Do Until TaskCancle
            'If TelemetryEnable Then
            '    'TRes.Append("T(")
            '    'TRes.Append(TelemetryDataV(Telemetry_Slot_Select, Telemetry_POS_Target).ToString)
            '    'TRes.Append(",")
            '    'TRes.Append(TelemetryDataS(Telemetry_Slot_Select, Telemetry_POS_RPM_Actual))
            '    'TRes.Append(",")
            '    'TRes.Append(TelemetryDataS(Telemetry_Slot_Select, Telemetry_CUR_Command))
            '    'TRes.Append(",")
            '    'TRes.Append(TelemetryDataS(Telemetry_Slot_Select, Telemetry_CUR_Measured))
            '    'TRes.Append(")")
            '    'TransferBuffer = Ascii.GetBytes(TRes.ToString)
            '    'Await NETTelemetry.SendAsync(TransferBuffer, TransferBuffer.Length, TelemetryEndpoint)
            '    'Await NETTelemetry.SendAsync(TransferBuffer, TransferBuffer.Length, PCEndpoint)
            '    'TRes.Clear()

            '    If IORegister(3, Reg.IO_P) = 4 Then
            '        'NextionUpdate(4)
            '    End If
            '    HaveData = False
            '    'If ControlRegister(Reg.Game_Controller_Type) = 0 Then
            '    '    If TelemetryDataS(0, 9).Length > 0 Then
            '    '        TRes.Append("J(")
            '    '        TRes.Append(TelemetryDataS(0, 9))
            '    '        TRes.Append(")")
            '    '        HaveData = True
            '    '    End If
            '    'End If

            '    'If ControlRegister(Reg.Game_Controller_Type) = 1 Then
            '    '    If GameControllerFound Then
            '    '        TRes.Append("J(")
            '    '        For i As Integer = 0 To ZCon.AxisCount - 1
            '    '            Select Case i
            '    '                Case = 0
            '    '                    TRes.Append(Math.Truncate(AxisArray(i) * 1024).ToString)
            '    '                Case = 1
            '    '                    COMB = Math.Truncate(AxisArray(i) * 65536.0)
            '    '                    If COMB > 32768 Then
            '    '                        P_Brake = (COMB - 32768) / 128
            '    '                    Else
            '    '                        P_Brake = 0
            '    '                    End If
            '    '                    If COMB <= 32768 Then
            '    '                        P_Gas = 256 - (COMB / 128)
            '    '                    Else
            '    '                        P_Gas = 0
            '    '                    End If
            '    '                    TRes.Append(P_Brake.ToString)
            '    '                    TRes.Append(",")
            '    '                    TRes.Append(P_Gas.ToString)
            '    '                Case Else
            '    '                    TRes.Append(Math.Truncate(AxisArray(i) * 255).ToString)

            '    '            End Select
            '    '            TRes.Append(",")
            '    '        Next
            '    '        BTNValue += 1
            '    '        If BTNValue > 250 Then BTNValue = 0
            '    '        'BTNValue = 0

            '    '        'For i As Integer = 0 To ZCon.ButtonCount - 1
            '    '        'If i < 16 Then
            '    '        'If BTNArray(i) Then
            '    '        'BTNValue += 2 ^ i
            '    '        'End If
            '    '        'End If
            '    '        'Next
            '    '        'BTNValue = 256
            '    '        TRes.Append(BTNValue.ToString)
            '    '        TRes.Append(")")
            '    '        HaveData = True
            '    '        'TransferBuffer = Ascii.GetBytes(TRes.ToString)
            '    '        'Debug.WriteLine(TRes.ToString)
            '    '    End If
            '    'End If

            '    'If HaveData Then
            '    'TransferBuffer = Ascii.GetBytes(TRes.ToString)
            '    'Await NETTelemetry.SendAsync(TransferBuffer, TransferBuffer.Length, TelemetryEndpoint)
            '    'Await NETTelemetry.SendAsync(TransferBuffer, TransferBuffer.Length, PCEndpoint)
            '    'End If
            '    TRes.Clear()
            'End If

            If ControlRegister(Reg.Game_Controller_Type) = 1 Then
                If Not GameControllerFound Then
                    If True Then ' --- use "RawGameController" ---
                        If RawGameController.RawGameControllers IsNot Nothing Then
                            If RawGameController.RawGameControllers.Count > 0 Then
                                GameControllerFound = True
                                ZCon = RawGameController.FromGameController(RawGameController.RawGameControllers.First())
                                ReDim BTNArray(ZCon.ButtonCount)
                                ReDim AxisArray(ZCon.AxisCount)
                                ReDim SWArray(ZCon.SwitchCount)
                                Debug.Write(ZCon.DisplayName)
                                Debug.Write(" VID:" & Convert.ToInt32(ZCon.HardwareVendorId, 16))
                                Debug.Write(" PID:" & Convert.ToInt32(ZCon.HardwareProductId, 16))
                                Debug.Write(" BTN:" & BTNArray.Length.ToString)
                                Debug.Write(" AXIS:" & AxisArray.Length.ToString)
                                Debug.Write(" SW:" & SWArray.Length.ToString)
                                Debug.Write(" FFB:" & ZCon.ForceFeedbackMotors.Count)
                                Debug.WriteLine(" SHC:" & ZCon.SimpleHapticsControllers.Count)
                                'FFBX = ZCon.ForceFeedbackMotors.First()

                                'Dim z As Windows.Gaming.Input.ForceFeedback.IForceFeedbackEffect

                                'FFBX.LoadEffectAsync(z)
                                'z = FFBX
                                'z.Gain = 1

                                'Debug.WriteLine(z.State)
                                'FFBX.LoadEffectAsync(New Windows.Gaming.Input.ForceFeedback.IForceFeedbackEffect(gain)
                            End If
                        End If
                    End If
                Else
                    If True Then
                        ZCon.GetCurrentReading(BTNArray, SWArray, AxisArray)
                    End If
                End If
            End If
            If ControlRegister(Reg.Game_Controller_Type) = 2 Then
                If Not GameControllerFound Then
                    If True Then ' --- use "RawGameController" ---
                        If RawGameController.RawGameControllers IsNot Nothing Then
                            If RawGameController.RawGameControllers.Count > 0 Then
                                GameControllerFound = True
                                ZWheel = RacingWheel.FromGameController(RawGameController.RawGameControllers.First())

                                Debug.WriteLine(RawGameController.RawGameControllers.First().DisplayName)
                                Debug.WriteLine(ZWheel.GetButtonLabel(RacingWheelButtons.Button1))
                            End If
                        End If
                    End If
                Else
                    If True Then
                        Debug.WriteLine(ZWheel.GetCurrentReading.Wheel)
                    End If
                End If
            End If


            IO_CNT += 1
            If IO_CNT = 10 Then
                'WriteCommand(Reg.Device_SystemIO, "R")
                'GPIOPIN(StatusLED).Write(Windows.Devices.Gpio.GpioPinValue.High)
            End If
            If IO_CNT > 20 Then
                IO_CNT = 0
                'GPIOPIN(StatusLED).Write(Windows.Devices.Gpio.GpioPinValue.Low)
                If ControlRegister(Register.Coin_Shadow) <> IORegister(Reg.Device_SystemIO, CoinIOSlot) Then
                    IO_TEMP = 0
                    If IORegister(Reg.Device_SystemIO, CoinIOSlot) > ControlRegister(Register.Coin_Shadow) Then
                        IO_TEMP = IORegister(Reg.Device_SystemIO, CoinIOSlot) - ControlRegister(Register.Coin_Shadow)
                    End If
                    If IORegister(Reg.Device_SystemIO, CoinIOSlot) < ControlRegister(Register.Coin_Shadow) Then
                        IO_TEMP = (255 - ControlRegister(Register.Coin_Shadow)) + IORegister(Reg.Device_SystemIO, CoinIOSlot) + 1
                    End If
                    ControlRegister(Register.Coin_Shadow) = IORegister(Reg.Device_SystemIO, CoinIOSlot)
                    ControlRegister(Register.Coin_Counter) += IO_TEMP
                    If BinaryMode Then
                        ExtIOEvent = True
                        Converter_Uint16 = Convert.ToUInt16(ControlRegister(Register.Coin_Counter))
                        ExtIORespondByte(6) = (Converter_Uint16 >> 8) And &HFF
                        ExtIORespondByte(7) = Converter_Uint16 And &HFF
                    Else
                        WriteRespondBinary(System.Text.Encoding.ASCII.GetBytes("COIN:" & ControlRegister(Register.Coin_Counter).ToString & vbCrLf), RespondTo.PC)
                    End If
                End If
                If ControlRegister(Register.Service_Shadow) <> IORegister(Reg.Device_SystemIO, ServiceIOSlot) Then
                    IO_TEMP = 0
                    If IORegister(Reg.Device_SystemIO, ServiceIOSlot) > ControlRegister(Register.Service_Shadow) Then
                        IO_TEMP = IORegister(Reg.Device_SystemIO, ServiceIOSlot) - ControlRegister(Register.Service_Shadow)
                    End If
                    If IORegister(Reg.Device_SystemIO, ServiceIOSlot) < ControlRegister(Register.Service_Shadow) Then
                        IO_TEMP = (255 - ControlRegister(Register.Service_Shadow)) + IORegister(Reg.Device_SystemIO, ServiceIOSlot) + 1
                    End If
                    ControlRegister(Register.Service_Shadow) = IORegister(Reg.Device_SystemIO, ServiceIOSlot)
                    ControlRegister(Register.Service_Counter) += IO_TEMP
                    If BinaryMode Then
                        ExtIOEvent = True
                        Converter_Uint16 = Convert.ToUInt16(ControlRegister(Register.Service_Counter))
                        ExtIORespondByte(6) = (Converter_Uint16 >> 8) And &HFF
                        ExtIORespondByte(7) = Converter_Uint16 And &HFF
                    Else
                        WriteRespondBinary(System.Text.Encoding.ASCII.GetBytes("SERV:" & ControlRegister(Register.Service_Counter).ToString & vbCrLf), RespondTo.PC)
                    End If
                End If
                If ControlRegister(Register.IO_Shadow) <> IORegister(Reg.Device_SystemIO, RawIOSlot) Then
                    ControlRegister(Register.IO_Shadow) = IORegister(Reg.Device_SystemIO, RawIOSlot)
                    If BinaryMode Then
                        'ExtIOEvent = True
                        'ExtIORespondByte(9) = 0
                        'ExtIORespondByte(10) = 0
                    Else
                        WriteRespondBinary(System.Text.Encoding.ASCII.GetBytes("GINT:" & ControlRegister(Register.IO_Shadow).ToString & vbCrLf), RespondTo.PC)
                    End If
                End If
                If ExtIOEvent Then
                    ExtIOEvent = False
                    WriteRespondBinary(ExtIORespondByte, RespondTo.PC)
                End If
            End If

            Run_CNT += 1
            If Run_CNT = 50 Then
                GPIOPIN(StatusLED).Write(Windows.Devices.Gpio.GpioPinValue.High)
                DebugMSGEventTick = True
                Log.Dashboard_Update()
            End If
            If Run_CNT > 100 Then
                Run_CNT = 0
                GPIOPIN(StatusLED).Write(Windows.Devices.Gpio.GpioPinValue.Low)
                ' --- debug cmd display
                'Debug.WriteLine("current debug CMD " & Log.Data.System_Event)
                If EmergencySTOP Then
                    UseComm(0) = False
                    UseComm(1) = False
                    UseComm(2) = False
                    UseComm(3) = False
                End If

                If DisplayOK And (Not NextionUpdate_Inprogress) Then
                    If IORegister(3, Reg.IO_P) > 0 Then
                        WriteNextion("va0.val=0")
                    Else
                        WriteNextion("page 1")
                    End If

                    'If IORegister(3, Reg.IO_P) = 2 Then
                    '    If StartupSequence_Finish And LocalMotorRunState < 2 Then
                    '        If ManualMotorState < 2 Then
                    '            WriteNextion("page_control.va1.val=0")
                    '        Else
                    '            WriteNextion("page_control.va1.val=1")
                    '        End If
                    '    Else
                    '        WriteNextion("page_control.va1.val=10")
                    '    End If
                    'End If

                End If

                'FTP_KACounter += 1
                'If FTP_KACounter > 10 Then
                '    FTP.KeepAlive()
                '    FTP_KACounter = 0
                'End If

                'Debug.WriteLine("R:" & IORegister(Reg.Device_SystemIO, Reg.IO_R).ToString)
                'Debug.Write(" S:" & IORegister(Reg.Device_SystemIO, Reg.IO_S).ToString)
                'Debug.Write(" T:" & IORegister(Reg.Device_SystemIO, Reg.IO_T).ToString)
                'Debug.Write(" C:" & IORegister(Reg.Device_SystemIO, Reg.IO_C).ToString)
            End If



            If StartupSequence_Finish Then
                '--- detect game control command (motor ON/OFF)
                If ControlRegister(Register.MotorState_Request) > 0 Then
                    Debug.WriteLine("Motor state request " & Register.MotorState_Request.ToString)
                    If ControlRegister(Register.MotorControl_Busy) = 0 And ControlRegister(Register.MotorState_Parking) = 0 Then ' if not in middle of state change
                        If ControlRegister(Register.MotorState_Request) <> LocalMotorRunState Then
                            ControlRegister(Register.MotorControl_Busy) = 1
                            LocalMotorRunState = ControlRegister(Register.MotorState_Request)
                            If LocalMotorRunState > 2 Then LocalMotorRunState = 2
                            Log.Data.System_OPMode = LocalMotorRunState
                            If ControlRegister(Register.MotorState_Request) > 1 Then
                                OutputScale = 0.0
                                AxisMath.setUsage(0)
                            End If
                            SelectProfile = Profile_FAST
                            ControlRegister(Register.MotorState_Request) = 0
                            WriteRespondBinary(System.Text.Encoding.ASCII.GetBytes("MOTOR:" & LocalMotorRunState.ToString & vbCrLf), RespondTo.PC)
                            MotorOutputControl(LocalMotorRunState, SelectProfile, True)
                            WriteRespondBinary(System.Text.Encoding.ASCII.GetBytes("MOTOR:" & LocalMotorRunState.ToString & vbCrLf), RespondTo.PC)
                        Else
                            ControlRegister(Register.MotorState_Request) = 0
                            WriteRespondBinary(System.Text.Encoding.ASCII.GetBytes("MOTOR:" & LocalMotorRunState.ToString & vbCrLf), RespondTo.PC)
                            'Debug.WriteLine("Motor same state request - ignore")
                        End If
                    End If
                End If

                '--- Soft start (soft stop moved in to motor stste control)
                If LocalMotorRunState <> LocalMotorModifierState Then
                    If ControlRegister(Register.MotorControl_Busy) = 0 Then
                        If LocalMotorRunState > 1 Then
                            If OutputScale = 0 Then Debug.WriteLine("Soft Start : Begin ")
                            OutputScale += SoftStartScale
                            If OutputScale >= 100.0 Then
                                OutputScale = 100.0
                                LocalMotorModifierState = LocalMotorRunState
                                Debug.WriteLine("Soft Start : Finish ")
                            End If
                            AxisMath.setUsage(Math.Truncate(OutputScale))
                        Else
                            LocalMotorModifierState = LocalMotorRunState
                        End If
                    End If


                    'If LocalMotorRunState < 2 And LocalMotorModifierState > 1 Then
                    '    OutputScale -= SoftStopScale
                    '    If OutputScale <= 0.0 Then
                    '        OutputScale = 0.0
                    '        LocalMotorModifierState = LocalMotorRunState
                    '    End If
                    '    AxisMath.setUsage(Math.Truncate(OutputScale))
                    'End If

                    'If LocalMotorRunState < 2 And ControlRegister(Reg.CONTROL_BUSY) = 0 Then
                    'LocalMotorModifierState = LocalMotorRunState
                    'OutputScale = 0.0
                    'Debug.WriteLine(Log.Data.DOF0_POS & ":" & Log.Data.DOF1_POS & ":" & Log.Data.DOF2_POS & ":" & Log.Data.DOF3_POS & ":" & Log.Data.DOF4_POS & ":" & Log.Data.DOF5_POS)
                End If

                ' --- Stall (Overheat) protection --- must enable telemetry fot this function to work
                For sidx As Integer = 0 To 3
                    stallDistant(sidx) = Log.Data.Axis_RawPosition(sidx) - Log.Data.Axis_APOS(sidx)
                    If Log.Data.Axis_APOS(sidx) = MotorPOSShadow(0) Then
                        If Math.Abs(stallDistant(sidx)) > stallMargin Then stallDelayCounter(sidx) += 1
                    Else
                        stallDelayCounter(sidx) = sidx
                    End If
                    MotorPOSShadow(sidx) = Log.Data.Axis_APOS(sidx)
                    If stallDelayCounter(sidx) > stallDelayLimit Then
                        If DebugMSGEventTick Then
                            'Debug.WriteLine("Stall " & sidx.ToString)
                        End If
                    End If
                Next


                ' --- wheel feedback (from Game)
                If ControlRegister(Register.Wheel_Velocity) <> ControlRegister(Register.Wheel_Velocity_Shadow) Then
                    If ControlRegister(Register.Wheel_Velocity) > 32767.0 Then ControlRegister(Register.Wheel_Velocity) = 32767.0
                    If ControlRegister(Register.Wheel_Velocity) < -32767.0 Then ControlRegister(Register.Wheel_Velocity) = -32767.0
                    ControlRegister(Register.Wheel_Velocity_Shadow) = ControlRegister(Register.Wheel_Velocity)
                    Converter_int16 = Math.Truncate(ControlRegister(Register.Wheel_Velocity))
                    Array_16bit = BitConverter.GetBytes(Converter_int16)
                    WheelCommandByte(3) = Array_16bit(1)
                    WheelCommandByte(4) = Array_16bit(0)
                    WheelCommandUpdate = True
                    Debug.WriteLine("Wheel_Velocity:" & Converter_int16.ToString)
                End If
                If ControlRegister(Register.Wheel_LED) <> ControlRegister(Register.Wheel_LED_Shadow) Then
                    If ControlRegister(Register.Wheel_LED) > 65535.0 Then ControlRegister(Register.Wheel_LED) = 65535.0
                    ControlRegister(Register.Wheel_LED_Shadow) = ControlRegister(Register.Wheel_LED)
                    Converter_Uint16 = Math.Truncate(ControlRegister(Register.Wheel_LED))
                    Array_16bit = BitConverter.GetBytes(Converter_Uint16)
                    WheelCommandByte(5) = Array_16bit(1)
                    WheelCommandByte(6) = Array_16bit(0)
                    WheelCommandUpdate = True
                End If
                If WheelCommandUpdate Then
                    RingWriteIndex(Reg.Device_Wheel) += 1
                    If RingWriteIndex(Reg.Device_Wheel) >= RingBufferSize Then RingWriteIndex(Reg.Device_Wheel) = 0
                    RingByteBuffer(Reg.Device_Wheel, RingWriteIndex(Reg.Device_Wheel)) = WheelCommandByte
                    WheelCommandUpdate = False
                End If


                ' --- drive param update
                If ControlRegister(Register.Setting_Update) = 100 Then
                    If ControlRegister(Register.MotorState_Request) = 0 Then
                        If ControlRegister(Register.MotorControl_Busy) = 0 And ControlRegister(Register.MotorState_Parking) = 0 Then
                            ControlRegister(Register.Setting_Update) = 99
                            Debug.WriteLine("Parameter update begin")
                            For i As Integer = 0 To 7
                                If ForceProfileUpdate(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.Axis(i).RunProfile, True) > 0 Then
                                    ControlRegister(Register.Setting_Update) = -1
                                    Exit For
                                End If
                            Next
                            If ControlRegister(Register.Setting_Update) > 0 Then ControlRegister(Register.Setting_Update) = 0
                            Debug.WriteLine("Parameter update finish")
                        End If
                    End If
                End If

            End If

            ' --- remote pc ip safety (lock to remote ip when receive sys.use command)
            If EndpointINIT = 1 Then
                EndpointINIT += 1
                CommandReceverTask.CancelAsync()
                Debug.WriteLine("Stop - UDP Command Receiver")
                While CommandReceverTask.IsBusy
                    Task.Delay(500).Wait()
                    Debug.Write(".")
                End While
                Debug.WriteLine(".")
                CommandReceverTask.RunWorkerAsync(NetBinding)
            End If

            'If ControlRegister(Reg.GAME_CONTROL) > 0 Then
            '    If StartupSequence_Finish Then
            '        If ControlRegister(Reg.CONTROL_BUSY) = 0 And ManualMotorState < 2 Then ' if not in middle of state change and [game control state = off]
            '            GameMotorState = ControlRegister(Reg.GAME_CONTROL)
            '            ControlRegister(Reg.CONTROL_BUSY) = 1
            '            If GameMotorState > 2 Then GameMotorState = 2
            '            If ReqStateBuffer Then ControlRegister(Reg.GAME_CONTROL) = 0
            '            MotorOutputControl(GameMotorState, Profile_FAST)
            '            If Not ReqStateBuffer Then ControlRegister(Reg.GAME_CONTROL) = 0
            '        Else
            '            If ManualMotorState > 1 Then ControlRegister(Reg.GAME_CONTROL) = 0
            '        End If
            '    End If
            'End If

            '--- detect manual command (motor ON/OFF) ---
            'If IORegister(3, IO_M) > 0 Then
            '    If StartupSequence_Finish Then
            '        If ControlRegister(Reg.CONTROL_BUSY) = 0 And GameMotorState < 2 Then ' if not in middle of state change and [game control state = off]
            '            ManualMotorState = IORegister(3, IO_M)
            '            ControlRegister(Reg.CONTROL_BUSY) = 1
            '            If ManualMotorState > 2 Then ManualMotorState = 2
            '            If ReqStateBuffer Then IORegister(3, IO_M) = 0
            '            MotorOutputControl(ManualMotorState, Profile_SLOW)
            '            If Not ReqStateBuffer Then IORegister(3, IO_M) = 0
            '        Else
            '            If GameMotorState > 1 Then IORegister(3, IO_M) = 0
            '        End If
            '    End If
            'End If

            '--- MANUAL_CONTROLL
            'If ControlRegister(Reg.GAME_CONTROL) < 2 And ControlRegister(Reg.MANUAL_CONTROL) > 1 And ControlRegister(Reg.CONTROL_BUSY) = 0 Then
            '    CMD_Manual = ""
            '    If IORegister(3, IO_X) > 0 Then
            '        If IORegister(3, IO_X) = 2 Then ControlRegister(Register.Axis0_ManualPOS) += 1024
            '        If IORegister(3, IO_X) = 1 Then ControlRegister(Register.Axis0_ManualPOS) -= 1024
            '        CMD_Manual = "[0]drive(0).pos(0," & ControlRegister(Register.Axis0_ManualPOS).ToString & ")"
            '    End If
            '    If IORegister(3, IO_Y) > 0 Then
            '        If IORegister(3, IO_Y) = 2 Then ControlRegister(Register.Axis1_ManualPOS) += 1024
            '        If IORegister(3, IO_Y) = 1 Then ControlRegister(Register.Axis1_ManualPOS) -= 1024
            '        CMD_Manual = "[0]drive(0).pos(1," & ControlRegister(Register.Axis1_ManualPOS).ToString & ")"
            '    End If
            '    If IORegister(3, IO_Z) > 0 Then
            '        If IORegister(3, IO_Z) = 2 Then ControlRegister(Register.Axis2_ManualPOS) += 1024
            '        If IORegister(3, IO_Z) = 1 Then ControlRegister(Register.Axis2_ManualPOS) -= 1024
            '        CMD_Manual = "[0]drive(1).pos(0," & ControlRegister(Register.Axis2_ManualPOS).ToString & ")"
            '    End If
            '    If IORegister(3, IO_A) > 0 Then
            '        If IORegister(3, IO_A) = 2 Then ControlRegister(Register.Axis3_ManualPOS) += 1024
            '        If IORegister(3, IO_A) = 1 Then ControlRegister(Register.Axis3_ManualPOS) -= 1024
            '        CMD_Manual = "[0]drive(1).pos(1," & ControlRegister(Register.Axis3_ManualPOS).ToString & ")"
            '    End If
            '    If CMD_Manual <> "" Then CommandProcessor(CMD_Manual)
            '    IORegister(3, IO_X) = 0
            '    IORegister(3, IO_Y) = 0
            '    IORegister(3, IO_Z) = 0
            '    IORegister(3, IO_A) = 0
            'End If
            'If IORegister(3, IO_P) <> NextionPage_Shadow Then
            '    NextionPage_Shadow = IORegister(3, IO_P)
            '    NextionUpdate(NextionPage_Shadow)
            'End If


            'If IORegister(3, Reg.IO_T) > 0 Then
            '    Select Case Convert.ToInt32(IORegister(3, Reg.IO_T))
            '        Case = 0
            '             Telemetry_DRV_Select = -1
            '             Telemetry_AXIS_Select = 0
            '         Case = 1
            '             Telemetry_DRV_Select = 0
            '             Telemetry_AXIS_Select = 0
            '       Case = 2
            '            Telemetry_DRV_Select = 0
            '            Telemetry_AXIS_Select = 1
            '        Case = 3
            '            Telemetry_DRV_Select = 1
            '            Telemetry_AXIS_Select = 0
            '        Case = 4
            '            Telemetry_DRV_Select = 1
            '            Telemetry_AXIS_Select = 1
            '    End Select
            '    IORegister(3, Reg.IO_T) = 0
            'End If
            'Wheel.WheelPOS = xRandom.Next(65535)
            If Not Wheel.Equals(Wheel_Shadow) Then
                Wheel_Shadow = Wheel
                If BinaryMode Then
                    'WheelRespondByte(0) = 120
                    'WheelRespondByte(1) = 68
                    WheelRespondByte(2) = 16
                    Converter_Uint16 = Wheel.WheelPOS
                    WheelRespondByte(3) = (Converter_Uint16 >> 8) And &HFF
                    WheelRespondByte(4) = Converter_Uint16 And &HFF
                    WheelRespondByte(5) = Wheel.AnalogCH_1
                    WheelRespondByte(6) = Wheel.AnalogCH_2
                    WheelRespondByte(7) = Wheel.AnalogCH_3
                    WheelRespondByte(8) = Wheel.AnalogCH_4
                    Converter_Uint16 = Wheel.Button
                    WheelRespondByte(9) = (Converter_Uint16 >> 8) And &HFF
                    WheelRespondByte(10) = Converter_Uint16 And &HFF
                    WriteRespondBinary(WheelRespondByte, RespondTo.PC)

                    'WheelCommandByte = WheelRespondByte
                    'WheelCommandUpdate = True
                Else
                    xStr.Clear()
                    xStr.Append("CONT:")
                    xStr.Append(Wheel.WheelPOS)
                    xStr.Append(",")
                    xStr.Append(Wheel.AnalogCH_1)
                    xStr.Append(",")
                    xStr.Append(Wheel.AnalogCH_2)
                    xStr.Append(",")
                    xStr.Append(Wheel.AnalogCH_3)
                    xStr.Append(",")
                    xStr.Append(Wheel.AnalogCH_4)
                    xStr.Append(",")
                    xStr.Append(Wheel.Button)
                    WriteRespondBinary(System.Text.Encoding.ASCII.GetBytes(xStr.ToString), RespondTo.PC)
                End If
            End If
            DebugMSGEventTick = False
            Task.Delay(1).Wait()
        Loop
    End Sub

    Private Async Sub GPIOTask()
        SysEvent("Start - GPIO")
        GPIOPIN(2).SetDriveMode(Windows.Devices.Gpio.GpioPinDriveMode.Output)
        Do Until TaskCancle
            GPIOPIN(2).Write(Windows.Devices.Gpio.GpioPinValue.High)
            GPIOPIN(2).Write(Windows.Devices.Gpio.GpioPinValue.Low)
            Await Task.Delay(10)
        Loop
    End Sub

    ' Private Async Sub GPIOReadPoll()
    'Do Until TaskCancle
    'For i As Integer = 0 To GPIOInputStatus.Length - 1
    '
    '    Next
    '   Loop
    '  End Sub

    Private Function CommandProcessor(CMD As String) As Boolean

        Dim COMOK As Boolean = False
        'Dim RegisterWriteEvent As Boolean = False

        Dim SIDXA As Integer = 0
        Dim SIDXB As Integer = 0
        Dim SIDXC As Integer = 0
        Dim CMDIDString As String = ""
        Dim CMDError As Boolean = False
        Dim CMDValid As Boolean = False
        Dim CMDFound As Boolean = False
        Dim VALValid As Boolean = False
        Dim VALString As String = ""
        Dim VALValue As Integer = 0
        Dim TARGETPointer As Integer = 0
        Dim CHANNELPointer As Integer = 0
        Dim SYSMode As Integer = 0
        Dim CIDLenght As Integer = 0
        Dim CMDCNT As Integer = 0

        Dim CMDQuery(10) As String
        Dim CMDQueryOriginal(10) As String
        Dim SCMDString As String = ""
        Dim QIDX As Integer = 0
        Dim CIDX As Integer = 0
        Dim CIDXShadow As Integer = 0
        Dim CMDLocked As Boolean = False

        CMDBUSY = True

        'performanceTimer_start = System.DateTime.Now.Ticks


        'Log.Data.CMD = CMD.Replace(",", ";")
        'Log.Data.System_Event = Log.Data.CMD
        'Log.Add()
        Log.Data.System_Event = CMD.Replace(",", ";")
        'Debug.WriteLine(Log.Data.System_Event)

        'read command sequence number
        SIDXA = CMD.IndexOf("[")
        SIDXB = CMD.IndexOf("]")
        If DebugEnable Then Debug.WriteLine(CMD)
        If SIDXA >= 0 And SIDXB > 0 And SIDXB - SIDXA > 1 Then
            CMDError = False
            SIDXA += 1
            CMDIDString = CMD.Substring(SIDXA, SIDXB - SIDXA).Trim
            If Integer.TryParse(CMDIDString, RecvCMDID) Then
                CMD = CMD.Substring(SIDXB + 1)
                CMDRes.Append("[")
                CMDRes.Append(RecvCMDID.ToString)
                CMDRes.Append("]")
                CIDLenght = CMDRes.Length
            Else
                CMDError = True
                CMDRes.Append("invalid SeqID")
            End If
        Else
            CMDError = True
            CMDRes.Append("command error")
        End If

        'if command sequence is valid ... contiune
        If Not CMDError Then
            'split multiple command
            If CMD.Contains(":") Then
                QIDX = 0
                For Each MCMD As String In CMD.Split(":")
                    QIDX += 1
                    CMDQuery(QIDX) = MCMD.Trim
                Next
            Else
                QIDX = 1
                CMDQuery(1) = CMD.Trim
            End If
            'process
            For i As Integer = 1 To QIDX
                CIDXShadow = -1
                CMDValid = False
                SYSMode = -1
                CMDCNT = 0
                CMDLocked = False
                For j As Integer = 0 To Dict.Length - 1
                    CIDX = CMDQuery(i).ToUpper.IndexOf(Dict(j))
                    'CMDFound = CMDQuery(i).ToUpper.StartsWith(Dict(j))
                    CMDFound = False
                    If CIDX >= 0 Then
                        CMDFound = True
                        CMDValid = True
                        If CIDXShadow > CIDX Then
                            CMDValid = False
                        End If
                        CIDXShadow = CIDX
                    End If
                    If CMDFound Then
                        SCMDString = CMDQuery(i).Substring(CIDX)
                        VALValid = False
                        SIDXA = SCMDString.IndexOf("(")
                        SIDXB = SCMDString.IndexOf(")")
                        If j = 7 Then
                            SIDXB = SCMDString.LastIndexOf(")")
                            CIDX = SIDXB
                        End If
                        SIDXC = SCMDString.IndexOf(".")
                        If SIDXA >= 0 And SIDXB > 0 And SIDXB - SIDXA > 1 Then
                            VALValid = True
                            SIDXA += 1
                        End If
                        If Not CMDLocked Then
                            Select Case j
                                Case 0 'SYS
                                    SYSMode = 0
                                Case 1 To 5 'GPIO,I2C,SPI,UART,DRIVE
                                    SYSMode = j
                                    If VALValid And SIDXC > SIDXB Then
                                        VALString = SCMDString.Substring(SIDXA, SIDXB - SIDXA)
                                        If VALString.All(AddressOf Char.IsDigit) Then
                                            TARGETPointer = Convert.ToInt32(VALString)
                                        Else
                                            'CMDValid = False
                                        End If
                                    Else
                                        CMDValid = False
                                    End If
                                Case 6 'READ
                                    If VALValid Then
                                        SYSMode += 10
                                        VALString = ""
                                        VALValue = 0
                                        VALValid = Integer.TryParse(SCMDString.Substring(SIDXA, SIDXB - SIDXA), VALValue)
                                        VALString = SCMDString.Substring(SIDXA, SIDXB - SIDXA)
                                        CMDLocked = True
                                    Else
                                        CMDValid = False
                                    End If
                                    CMDLocked = True
                                Case 7 'WRITE
                                    If VALValid Then
                                        SYSMode += 20
                                        VALString = ""
                                        VALValue = 0
                                        VALValid = Integer.TryParse(SCMDString.Substring(SIDXA, SIDXB - SIDXA), VALValue)
                                        VALString = SCMDString.Substring(SIDXA, SIDXB - SIDXA)
                                        CMDLocked = True
                                    Else
                                        CMDValid = False
                                    End If
                                Case 8 'USE
                                    SYSMode += 80
                                    CMDLocked = True
                                    If VALValid Then
                                        VALString = SCMDString.Substring(SIDXA, SIDXB - SIDXA)
                                    Else
                                        CMDValid = False
                                    End If
                                Case 9 'DEVICE
                                    SYSMode += 90
                                Case 10 'LIST
                                    SYSMode += 900
                                    CMDLocked = True
                                Case 11 'POS
                                    SYSMode += 70
                                    CMDLocked = True
                                    VALString = ""
                                    VALValue = 0
                                    'VALValid = Integer.TryParse(SCMDString.Substring(SIDXA, SIDXB - SIDXA), VALValue)
                                    VALString = SCMDString.Substring(SIDXA, SIDXB - SIDXA)
                               'Case 12 'STREAM 
                                    '    If VALValid Then
                                    '        SYSMode += 60
                                    '        VALString = ""
                                    '        VALValue = 0
                                    '        VALValid = Integer.TryParse(SCMDString.Substring(SIDXA, SIDXB - SIDXA), VALValue)
                                    '        CMDLocked = True
                                    '    Else
                                    '        CMDValid = False
                                    '    End If
                                Case 12 'STOP
                                    SYSMode += 60
                            End Select
                        End If

                    End If
                    'If Not CMDValid Then Exit For
                Next
                If CMDValid Then ' --- command execution ---
                    Select Case SYSMode
                        Case 60 'SYS.STOP ------ old 'SYS.STREAM(X) turn telemetry mode on/off 
                            SysEvent("!!! - EMERGENCY STOP - !!!")
                            EmergencySTOP = True
                            UseComm(0) = False
                            UseComm(1) = False
                            UseComm(2) = False
                            UseComm(3) = False
                            '0:off
                            '1:Drive-0 Motor-0 
                            '2:Drive-0 Motor-1 
                            '3:Drive-1 Motor-0
                            '4:Drive-1 Motor-1

                            'old (value 65)
                            'DRIVE(x).STREAM(x) 0:Disable-CH0 1:Enable-CH0 2:Disable-CH1 3:Enable-CH1
                            'DRIVE(x).STREAM(z) "z" is 10 bit flag for control telemetry

                            'If VALValid Then

                            '    Select Case VALValue
                            '        Case = 0
                            '            Telemetry_DRV_Select = -1
                            '            Telemetry_AXIS_Select = 0
                            '        Case = 1
                            '            Telemetry_DRV_Select = 0
                            '            Telemetry_AXIS_Select = 0
                            '        Case = 2
                            '            Telemetry_DRV_Select = 0
                            '            Telemetry_AXIS_Select = 1
                            '        Case = 3
                            '            Telemetry_DRV_Select = 1
                            '            Telemetry_AXIS_Select = 0
                            '        Case = 4
                            '            Telemetry_DRV_Select = 1
                            '            Telemetry_AXIS_Select = 1
                            '    End Select

                            '    If VALValue > 0 Then
                            '        TelemetryEnable = True
                            '        Debug.WriteLine("Telemetry ON (drive:" & Telemetry_DRV_Select & " motor:" & Telemetry_AXIS_Select & ")")
                            '    Else
                            '        TelemetryEnable = False
                            '        Debug.WriteLine("Telemetry OFF")
                            '    End If
                            'End If
                            'Debug.WriteLine("STREAM MODE:" & TARGETPointer.ToString & "," & VALValue)

                        Case 80 'SYS.USE(x.x.x.x) 'change respond ip address                              
                            If EndpointINIT = 0 Then
                                If VALValid Then
                                    If VALString.Count(Function(c As Char) c = ".") <> 3 Then VALValid = False
                                    If VALValid Then
                                        If VALString.Contains(",") Then
                                            CommandReturnPort = Convert.ToUInt16((VALString.Substring(VALString.IndexOf(",") + 1)))
                                            VALString = VALString.Substring(0, VALString.IndexOf(",")).Trim
                                            Debug.WriteLine(VALString)
                                            Debug.WriteLine(CommandReturnPort.ToString)
                                        End If
                                        If System.Net.IPAddress.TryParse(VALString, EndpoinAddress) Then
                                            'PCEndpoint = New System.Net.IPEndPoint(EndpoinAddress, CommandTXPort)
                                            'PCEndpoint = New System.Net.IPEndPoint(EndpoinAddress, NetConfig.Control_PortTXTarget)
                                            PCEndpoint = New System.Net.IPEndPoint(EndpoinAddress, CommandReturnPort)
                                            'TelemetryEndpoint = New System.Net.IPEndPoint(EndpoinAddress, TelemetryTXPort)
                                            'Try
                                            'NETRecever = New Net.Sockets.UdpClient(EndpoinAddress.AddressFamily)
                                            ''NETRecever.Client.Bind(New System.Net.IPEndPoint(LocalAddress, CommandRXPort))
                                            'NETRecever.Client.Bind(New System.Net.IPEndPoint(LocalAddress, NetConfig.Control_PortTXTarget))
                                            'Catch ex As Exception
                                            ''NETRecever = New Net.Sockets.UdpClient(CommandRXPort)
                                            'NETRecever = New Net.Sockets.UdpClient(NetConfig.Control_PortTXTarget)
                                            'End Try
                                            Debug.WriteLine("Set Host " & PCEndpoint.Address.ToString & ":" & PCEndpoint.Port.ToString)
                                            NetBinding.IPBinding_A = EndpoinAddress
                                            EndpointINIT += 1




                                        End If

                                    Else
                                        VALValid = False
                                    End If
                                    Debug.WriteLine("Set Host Control to IP:" & VALString)

                                End If
                                CMDValid = VALValid
                            End If
                        Case 10 'SYS.READ(RegisterNo) 'read internal control register (register name in RegNAME)
                            If CMDLocked Then
                                If VALValid Then
                                    If VALValue <= ControlRegister.Length Then
                                        CMDRes.Append("REGISTER(")
                                        CMDRes.Append(VALValue.ToString)
                                        CMDRes.Append(")=")
                                        Select Case VALValue ' trap lowlevel hardware read
                                            Case 0 : CMDRes.Append("A maid is mythical being that all of us have heard about, but never seen. - ZUN")
                                            Case 72 : CMDRes.Append(IORegister(Reg.Device_SystemIO, Reg.IO_H).ToString)
                                            Case 85 : CMDRes.Append(IORegister(Reg.Device_SystemIO, Reg.Register.Axis3_ExtEncoder).ToString)
                                            Case 86 : CMDRes.Append("0") ' encoder input 4 not exist
                                            Case 87 : CMDRes.Append("0") ' encoder input 5 not exist
                                            Case 88 : CMDRes.Append(IORegister(Reg.Device_SystemIO, Reg.Register.Axis0_ExtEncoder).ToString)
                                            Case 89 : CMDRes.Append(IORegister(Reg.Device_SystemIO, Reg.Register.Axis1_ExtEncoder).ToString)
                                            Case 90 : CMDRes.Append(IORegister(Reg.Device_SystemIO, Reg.Register.Axis2_ExtEncoder).ToString)
                                            Case Else
                                                CMDRes.Append(ControlRegister(VALValue).ToString)
                                        End Select
                                    Else
                                        Debug.Write("Index out of range :" & VALValue.ToString)
                                    End If
                                Else
                                    Debug.Write("Invalid format :" & SCMDString)
                                End If
                            End If
                        Case 20 'SYS.WRITE(RegisterNo,Value) 'write to internal control register (register name in RegNAME)
                            '--- reserve register ---
                            'RegNo 1000 is IR  output (data in 6-digit string format)
                            'RegNo 1001 is ESC output (data in 6-digit string format)
                            'RegNo 1100 is Log name (data in string format)
                            If CMDLocked Then
                                If VALString.Contains(",") Then
                                    VALValid = Integer.TryParse(VALString.Substring(0, VALString.IndexOf(",")), VALValue)
                                    If VALValid Then TARGETPointer = VALValue
                                    VALValid = Integer.TryParse(VALString.Substring(VALString.IndexOf(",") + 1), VALValue)
                                    'Debug.WriteLine("REG:" & TARGETPointer & " Value:" & VALValue)
                                    If TARGETPointer <= ControlRegister.Length And VALValid Then
                                        If Reg.ControlPermission(TARGETPointer) Then
                                            ControlRegister(TARGETPointer) = VALValue
                                            'If TARGETPointer = Register.Output_Percentage Then AxisMath.setUsage(ControlRegister(TARGETPointer))
                                        Else
                                            CMDRes.Append("Register(")
                                            CMDRes.Append(TARGETPointer.ToString)
                                            CMDRes.Append(") permission error")
                                            SysEvent("Write permission error " & TARGETPointer.ToString)
                                        End If

                                    Else
                                        VALString = VALString.Substring(VALString.IndexOf(",") + 1).Trim
                                        If VALString <> "" Then
                                            Select Case TARGETPointer
                                                Case 1000 ' heater control (format "XXXXXX" 0-9 one digit per channel)
                                                    If VALString.Length = 6 Then
                                                        CMDDataOut.Clear()
                                                        CMDDataOut.Append("L")
                                                        CMDDataOut.Append(VALString)
                                                        If Not EmergencySTOP Then WriteCommand(Reg.Device_SystemIO, CMDDataOut.ToString)
                                                        Log.Data.FAN_Output = VALString
                                                    End If
                                                Case 1001 ' fan control (format "XXXXXX" 0-9 one digit per channel)
                                                    If VALString.Length = 6 Then
                                                        CMDDataOut.Clear()
                                                        CMDDataOut.Append("F")
                                                        CMDDataOut.Append(VALString)
                                                        If Not EmergencySTOP Then WriteCommand(Reg.Device_SystemIO, CMDDataOut.ToString)
                                                        Log.Data.IR_Output = VALString
                                                    End If
                                                Case 1002 ' heater and fan combo control (format "AAAABBBB" 0-9 one digit per channel , A = heater , B = fan )
                                                    If VALString.Length = 8 Then
                                                        CMDDataOut.Clear()
                                                        CMDDataOut.Append("L")
                                                        CMDDataOut.Append(VALString.Substring(0, 4))
                                                        CMDDataOut.Append("00")
                                                        CMDDataOut.Append("F")
                                                        CMDDataOut.Append(VALString.Substring(4, 4))
                                                        CMDDataOut.Append("00")
                                                        If Not EmergencySTOP Then WriteCommand(Reg.Device_SystemIO, CMDDataOut.ToString)
                                                        Log.Data.FAN_Output = VALString.Substring(0, 4) & "00"
                                                        Log.Data.IR_Output = VALString.Substring(4, 4) & "00"
                                                    End If
                                                Case 1100
                                                    Log.setName(VALString)
                                                Case 1101
                                                    Debug.WriteLine("!!! --- DANGER --- !!!")
                                                    Debug.WriteLine("!!! --- DANGER --- !!!")
                                                    Debug.WriteLine("SIMTOOL MODE LOCK 16-BIT INT POS ONLY")
                                                    AxisMath.useSimtools(True)
                                                    Log.setName("SimTools")
                                                    Debug.WriteLine("offset mode : SimTools")
                                                    If VALValue > 0 Then
                                                        'AxisMath.useSimtools(True)
                                                        'Log.setName("SimTools")
                                                        'Debug.WriteLine("offset mode : SimTools")
                                                    Else
                                                        'AxisMath.useSimtools(False)
                                                        'Debug.WriteLine("offset mode : original")
                                                    End If
                                                Case Else
                                                    Debug.Write("Index out of range :" & VALValue.ToString)
                                            End Select
                                        End If

                                        'If TARGETPointer = 1000 Then
                                        '    VALString = VALString.Substring(VALString.IndexOf(",") + 1).Trim
                                        '    If VALString <> "" Then
                                        '        If VALString.Length = 6 Then
                                        '            CMDDataOut.Clear()
                                        '            CMDDataOut.Append("L")
                                        '            CMDDataOut.Append(VALString)
                                        '            WriteCommand(Reg.Device_SystemIO, CMDDataOut.ToString)
                                        '            Log.Data.FAN_Output = VALString
                                        '            'Debug.WriteLine("GPIO : " & CMDDataOut.ToString)
                                        '        End If
                                        '    End If
                                        'End If
                                        'If TARGETPointer = 1001 Then
                                        '    VALString = VALString.Substring(VALString.IndexOf(",") + 1).Trim
                                        '    If VALString <> "" Then
                                        '        If VALString.Length = 6 Then
                                        '            CMDDataOut.Clear()
                                        '            CMDDataOut.Append("F")
                                        '            CMDDataOut.Append(VALString)
                                        '            WriteCommand(Reg.Device_SystemIO, CMDDataOut.ToString)
                                        '            Log.Data.IR_Output = VALString
                                        '            'Debug.WriteLine("GPIO : " & CMDDataOut.ToString)
                                        '        End If
                                        '    End If
                                        'End If
                                        'If TARGETPointer = 1100 Then
                                        '    Log.setName(VALString.Substring(VALString.IndexOf(",") + 1).Trim)
                                        'End If
                                        'If TARGETPointer = 1101 Then
                                        '    If VALValue > 0 Then
                                        '        AxisMath.useSimtools(True)
                                        '        Log.setName("SimTools")
                                        '        Debug.WriteLine("offset mode : SimTools")
                                        '    Else
                                        '        AxisMath.useSimtools(False)
                                        '        Debug.WriteLine("offset mode : original")
                                        '    End If
                                        'End If
                                        'If TARGETPointer < 1000 And TARGETPointer > 1001 Then Debug.Write("Index out of range :" & VALValue.ToString)
                                    End If
                                End If
                            End If
                        Case 11 'GPIO(x).READ(x)
                            If VALString.Contains(",") Then

                            Else
                                If VALValid Then

                                Else
                                    CMDValid = False
                                End If

                            End If
                        Case 21 'GPIO(x).WRITE(x)
                            CMDValid = False
                        Case 25 'DRIVE(x).WRITE(value or string)
                            If True Then
                                If UseComm(TARGETPointer) Then
                                    If VALString.StartsWith("ss") Then
                                        'VALString = ""
                                        'CMDRes.Append("ERROR:Command disable")
                                        'CMDRes.Append("ODrive:Parameter Save")
                                    End If
                                    If VALString.StartsWith("sr") Then
                                        'VALString = ""
                                        'CMDRes.Append("ERROR:Command disable")
                                        'CMDRes.Append("ODrive:Reboot")
                                    End If
                                    'CommWriter(TARGETPointer).WriteString(VALString & ASCIIlineFeed)
                                    WriteCommand(TARGETPointer, VALString & ASCIIlineFeed)
                                    'If DebugEnable Then Debug.WriteLine("DRV(" & TARGETPointer.ToString & ") " & VALString)
                                Else
                                    CMDRes.Append("DRIVE(")
                                    CMDRes.Append(TARGETPointer.ToString)
                                    CMDRes.Append(") connection error")
                                End If
                            End If
                        Case 85 'DRIVE(x).USE(device name , BAUD rate)
                            If UseComm(TARGETPointer) Then
                                CMDRes.Append("ALREADY CONNECTED")
                            Else
                                If TARGETPointer <= TotalUART Then
                                    If VALString.Contains(",") Then
                                        If Integer.TryParse(VALString.Substring(VALString.IndexOf(",") + 1), VALValue) Then
                                            VALString = VALString.Substring(0, VALString.IndexOf(","))
                                            Select Case TARGETPointer
                                                Case 6
                                                    If VALValue <= 115200 Then
                                                        CommSET(UseComm, TARGETPointer, VALString, VALValue, 5, True).Wait()
                                                    Else
                                                        CommSET(UseComm, TARGETPointer, VALString, VALValue, 1, True).Wait()
                                                    End If
                                                Case Else
                                                    If VALValue <= 115200 Then
                                                        CommSET(UseComm, TARGETPointer, VALString, VALValue, 5, False).Wait()
                                                    Else
                                                        CommSET(UseComm, TARGETPointer, VALString, VALValue, 1, False).Wait()
                                                    End If
                                            End Select
                                        Else
                                            CMDRes.Append("Invalid Format")
                                        End If
                                    End If
                                    'CommSET(UseComm, TARGETPointer, VALString, 230400, 1).Wait()
                                Else
                                    CMDRes.Append("Invalid Device")
                                End If
                                'Select Case TARGETPointer
                                '    Case 0
                                '        'CommSET(UseComm, TARGETPointer, VALString, 230400, 1).Wait()
                                '    Case 1
                                '        'CommSET(UseComm, TARGETPointer, VALString, 230400, 1).Wait()
                                '    Case 2
                                '        'CommSET(UseComm, TARGETPointer, VALString, 230400, 1).Wait()
                                '    Case 3
                                '        'CommSET(UseComm, TARGETPointer, VALString, 230400, 1).Wait()
                                '    Case 4
                                '        'CommSET(UseComm, TARGETPointer, VALString, 115200, 5).Wait()
                                '    Case 5
                                '        'CommSET(UseComm, TARGETPointer, VALString, 9600, 5).Wait()
                                '        'CommSET(UseComm, TARGETPointer, VALString, 5).Wait()
                                '    Case Else
                                '        CMDRes.Append("Invalid Device")
                                'End Select
                                CMDRes.Append("DRIVE(")
                                CMDRes.Append(TARGETPointer.ToString)
                                CMDRes.Append(").USE=")
                                'If VerbrossEnable Then Debug.Write("Drive(" & TARGETPointer.ToString & ") = " & VALString)
                                If UseComm(TARGETPointer) Then
                                    CMDRes.Append("OK")
                                    COMOK = True
                                    'If VerbrossEnable Then Debug.WriteLine(" OK")
                                Else
                                    CMDRes.Append("ERROR")
                                    COMOK = False
                                    'If VerbrossEnable Then Debug.WriteLine(" ERROR")
                                End If

                            End If
                        Case 75 'DRIVE(x).POS(MotorID,Position) ---- position in ppr unit
                            If UseComm(TARGETPointer) Then
                                CMDDataOut.Clear()
                                If VALString.Contains(",") Then
                                    VALValid = Integer.TryParse(VALString.Substring(0, VALString.IndexOf(",")), VALValue)
                                    CHANNELPointer = VALValue
                                    If CHANNELPointer = 0 Or CHANNELPointer = 1 Then
                                        CMDDataOut.Append("t ")
                                        CMDDataOut.Append(CHANNELPointer.ToString)
                                        CMDDataOut.Append(" ")
                                        VALValid = Integer.TryParse(VALString.Substring(VALString.IndexOf(",") + 1), VALValue)
                                        If TARGETPointer = 0 And CHANNELPointer = 0 Then
                                            If Not ODrive.Motor(0).LimitOverride Then
                                                If VALValue > ODrive.Axis(0).RangePositive Then VALValue = ODrive.Axis(0).RangePositive
                                                If VALValue < ODrive.Axis(0).RangeNegative Then VALValue = ODrive.Axis(0).RangeNegative
                                                'If VALValue > Axis0_PRange Then VALValue = Axis0_PRange
                                                'If VALValue < Axis0_MRange Then VALValue = Axis0_MRange
                                            End If
                                            CMDDataOut.Append((VALValue + ControlRegister(Register.Axis0_Offset)).ToString)
                                        End If
                                        If TARGETPointer = 0 And CHANNELPointer = 1 Then
                                            If Not ODrive.Motor(1).LimitOverride Then
                                                If VALValue > ODrive.Axis(1).RangePositive Then VALValue = ODrive.Axis(1).RangePositive
                                                If VALValue < ODrive.Axis(1).RangeNegative Then VALValue = ODrive.Axis(1).RangeNegative
                                                'If VALValue > Axis1_PRange Then VALValue = Axis1_PRange
                                                'If VALValue < Axis1_MRange Then VALValue = Axis1_MRange
                                            End If
                                            CMDDataOut.Append((VALValue + ControlRegister(Register.Axis1_Offset)).ToString)
                                        End If
                                        If TARGETPointer = 1 And CHANNELPointer = 0 Then
                                            If Not ODrive.Motor(2).LimitOverride Then
                                                If VALValue > ODrive.Axis(2).RangePositive Then VALValue = ODrive.Axis(2).RangePositive
                                                If VALValue < ODrive.Axis(2).RangeNegative Then VALValue = ODrive.Axis(2).RangeNegative
                                                'If VALValue > Axis2_PRange Then VALValue = Axis2_PRange
                                                'If VALValue < Axis2_MRange Then VALValue = Axis2_MRange
                                            End If
                                            CMDDataOut.Append((VALValue + ControlRegister(Register.Axis2_Offset)).ToString)
                                        End If
                                        If TARGETPointer = 1 And CHANNELPointer = 1 Then
                                            If Not ODrive.Motor(3).LimitOverride Then
                                                If VALValue > ODrive.Axis(3).RangePositive Then VALValue = ODrive.Axis(3).RangePositive
                                                If VALValue < ODrive.Axis(3).RangeNegative Then VALValue = ODrive.Axis(3).RangeNegative
                                                'If VALValue > Axis3_PRange Then VALValue = Axis3_PRange
                                                'If VALValue < Axis3_MRange Then VALValue = Axis3_MRange
                                            End If
                                            CMDDataOut.Append((VALValue + ControlRegister(Register.Axis3_Offset)).ToString)
                                        End If
                                        CMDDataOut.Append(ASCIIlineFeed)
                                        '            CMDDataOut.Append("t 1 ")
                                        '            Axis_Temp = Math.Truncate(Axis1_Scale * VALValue)
                                        '            If Axis_Temp > Axis1_PRange Then Axis_Temp = Axis1_PRange
                                        '            If Axis_Temp < Axis1_MRange Then Axis_Temp = Axis1_MRange
                                        '            TelemetryDataV(1, Telemetry_POS_Target) = Axis_Temp
                                        '            CMDDataOut.Append((Axis_Temp + ControlRegister(Reg.Axis1_Offset)).ToString)
                                        '            CMDDataOut.Append(ASCIIlineFeed)
                                        '            'CommWriter(TARGETPointer).WriteString(CMDDataOut.ToString)
                                        WriteCommand(TARGETPointer, CMDDataOut.ToString)
                                    End If
                                Else
                                    CMDRes.Append("DRIVE(")
                                    CMDRes.Append(TARGETPointer.ToString)
                                    CMDRes.Append(") invalid command")
                                End If
                            Else
                                CMDRes.Append("DRIVE(")
                                CMDRes.Append(TARGETPointer.ToString)
                                CMDRes.Append(") connection error")
                            End If

                        'Case 75 'DRIVE(x).POS(axis0,axis1)
                        '    If UseComm(TARGETPointer) Then
                        '        CMDDataOut.Clear()
                        '        If VALString.Contains(",") Then
                        '            VALValid = Integer.TryParse(VALString.Substring(0, VALString.IndexOf(",")), VALValue)
                        '            CMDDataOut.Append("t 0 ")
                        '            Axis_Temp = Math.Truncate(Axis0_Scale * VALValue)
                        '            If Axis_Temp > Axis0_PRange Then Axis_Temp = Axis0_PRange
                        '            If Axis_Temp < Axis0_MRange Then Axis_Temp = Axis0_MRange
                        '            TelemetryDataV(0, Telemetry_POS_Target) = Axis_Temp
                        '            CMDDataOut.Append((Axis_Temp + ControlRegister(Reg.Axis0_Offset)).ToString)
                        '            CMDDataOut.Append(ASCIIlineFeed)
                        '            VALValid = Integer.TryParse(VALString.Substring(VALString.IndexOf(",") + 1), VALValue)
                        '            CMDDataOut.Append("t 1 ")
                        '            Axis_Temp = Math.Truncate(Axis1_Scale * VALValue)
                        '            If Axis_Temp > Axis1_PRange Then Axis_Temp = Axis1_PRange
                        '            If Axis_Temp < Axis1_MRange Then Axis_Temp = Axis1_MRange
                        '            TelemetryDataV(1, Telemetry_POS_Target) = Axis_Temp
                        '            CMDDataOut.Append((Axis_Temp + ControlRegister(Reg.Axis1_Offset)).ToString)
                        '            CMDDataOut.Append(ASCIIlineFeed)
                        '            'CommWriter(TARGETPointer).WriteString(CMDDataOut.ToString)
                        '            WriteCommand(TARGETPointer, CMDDataOut.ToString)
                        '            'If DebugEnable Then Debug.Write("DRV(" & TARGETPointer.ToString & ") POS " & VALString)
                        '        Else
                        '            CMDRes.Append("DRIVE(")
                        '            CMDRes.Append(TARGETPointer.ToString)
                        '            CMDRes.Append(") invalid command")
                        '        End If
                        '    Else
                        '        CMDRes.Append("DRIVE(")
                        '        CMDRes.Append(TARGETPointer.ToString)
                        '        CMDRes.Append(") connection error")
                        '    End If

                        Case 70 'SYS.POS(axis0,axis1,axis2,axis3,axis4,axis5)
                            If VALValid Then
                                If RecvCMDID <> SEQIDShadow Then
                                    SEQIDShadow = RecvCMDID
                                    'If RecvCMDID > 100 Then AxisMath.useSimtools(False)
                                End If
                                TempString = VALString.Split(",")
                                If TempString.Length < 6 Then
                                    CMDRes.Append("invalid command")
                                    Exit Select
                                End If

                                'lock at last position data when parking inprogress
                                If StateRegister(Register.MotorState_Parking) > 0 Then
                                    TempString(0) = Log.Data.DOF0_POS
                                    TempString(1) = Log.Data.DOF1_POS
                                    TempString(2) = Log.Data.DOF2_POS
                                    TempString(3) = Log.Data.DOF3_POS
                                    TempString(4) = Log.Data.DOF4_POS
                                    TempString(5) = Log.Data.DOF5_POS
                                End If

                                Output_Axis = AxisMath.GetAxisOutput(TempString(0), TempString(1), TempString(2), TempString(3), TempString(4), TempString(5))

                                Log.Data.DOF0_POS = TempString(0)
                                Log.Data.DOF1_POS = TempString(1)
                                Log.Data.DOF2_POS = TempString(2)
                                Log.Data.DOF3_POS = TempString(3)
                                Log.Data.DOF4_POS = TempString(4)
                                Log.Data.DOF5_POS = TempString(5)

                                CMDDataDrv0.Clear()
                                CMDDataDrv1.Clear()
                                CMDDataDrv2.Clear()
                                CMDAvailableDrv0 = False
                                CMDAvailableDrv1 = False
                                CMDAvailableDrv2 = False

                                If UseComm(0) Then '--- ODrive 1 (axis 1,2) output ---
                                    'CMDDataDrv0.Clear()
                                    If ODrive.Motor(0).Ready Then
                                        'Axis_Temp = Math.Truncate(Axis_Scale(0) * Output_Axis.A0)
                                        Axis_Temp = Axis_Scale(0) * Output_Axis.A0
                                        'Axis_Temp = AxisMath.MoveCap(PositionShadow(0), Math.Truncate(Axis0_Scale * Output_Axis.A0), SuddenMove_Limit)
                                        'Axis_Temp = AxisMath.MoveCap(PositionShadow(0), Axis_Temp, SuddenMove_Limit)
                                        If Axis_Temp > ODrive.Axis(0).RangePositive Then Axis_Temp = ODrive.Axis(0).RangePositive : Log.Data.Axis_ERR_INT(0) = 100 'Log.Data.Axis0_ERR_INT = "100:"
                                        If Axis_Temp < ODrive.Axis(0).RangeNegative Then Axis_Temp = ODrive.Axis(0).RangeNegative : Log.Data.Axis_ERR_INT(0) = 101 'Log.Data.Axis0_ERR_INT = "101:"
                                        TotalRevolution(0) += Math.Abs(Axis_Temp - PositionShadow(0))
                                        If Axis_Temp <> PositionShadow(0) Then TotalTime(0) += 25
                                        PositionShadow(0) = Axis_Temp
                                        'Log.Data.Axis0_CPOS = Math.Round(Axis_Temp / Axis0_PPD, 2).ToString
                                        Log.Data.Axis_CPOS(0) = Axis_Temp / ODrive.Axis(0).GearRatio
                                        Log.Data.Axis_RawPosition(0) = Axis_Temp
                                        'CMDDataDrv0.Append("t 0 ")
                                        'CMDDataDrv0.Append((Axis_Temp + ControlRegister(Reg.Axis0_Offset)).ToString)
                                        'CMDDataDrv0.Append(ASCIIlineFeed)
                                        Select Case ODrive.Map_DRV(0)
                                            Case 0 : CMDDataDrv0.Append(ODrive.TCmd(ODrive.Map_OUT(0))) : CMDDataDrv0.Append((Axis_Temp + MotorStallOffset(0) + ControlRegister(Register.Axis0_Offset)).ToString("F4")) : CMDDataDrv0.Append(ASCIIlineFeed) : CMDAvailableDrv0 = True
                                            Case 1 : CMDDataDrv1.Append(ODrive.TCmd(ODrive.Map_OUT(0))) : CMDDataDrv1.Append((Axis_Temp + MotorStallOffset(0) + ControlRegister(Register.Axis0_Offset)).ToString("F4")) : CMDDataDrv1.Append(ASCIIlineFeed) : CMDAvailableDrv1 = True
                                            Case 2 : CMDDataDrv2.Append(ODrive.TCmd(ODrive.Map_OUT(0))) : CMDDataDrv2.Append((Axis_Temp + MotorStallOffset(0) + ControlRegister(Register.Axis0_Offset)).ToString("F4")) : CMDDataDrv2.Append(ASCIIlineFeed) : CMDAvailableDrv2 = True
                                        End Select
                                    Else
                                        Axis_Temp = Axis_Scale(0) * Output_Axis.A0
                                        Log.Data.Axis_CPOS(0) = Axis_Temp / ODrive.Axis(0).GearRatio
                                        Log.Data.Axis_RawPosition(0) = Axis_Temp
                                    End If
                                    If ODrive.Motor(1).Ready Then
                                        'Axis_Temp = Math.Truncate(Axis_Scale(1) * Output_Axis.A1)
                                        Axis_Temp = Axis_Scale(1) * Output_Axis.A1
                                        'Axis_Temp = AxisMath.MoveCap(PositionShadow(1), Math.Truncate(Axis1_Scale * Output_Axis.A1), SuddenMove_Limit)
                                        'Axis_Temp = AxisMath.MoveCap(PositionShadow(1), Axis_Temp, SuddenMove_Limit)
                                        If Axis_Temp > ODrive.Axis(1).RangePositive Then Axis_Temp = ODrive.Axis(1).RangePositive : Log.Data.Axis_ERR_INT(1) = 100 'Log.Data.Axis1_ERR_INT = "100:"
                                        If Axis_Temp < ODrive.Axis(1).RangeNegative Then Axis_Temp = ODrive.Axis(1).RangeNegative : Log.Data.Axis_ERR_INT(1) = 101 'Log.Data.Axis1_ERR_INT = "101:"
                                        TotalRevolution(1) += Math.Abs(Axis_Temp - PositionShadow(1))
                                        If Axis_Temp <> PositionShadow(1) Then TotalTime(1) += 25
                                        PositionShadow(1) = Axis_Temp
                                        'Log.Data.Axis1_CPOS = Math.Round(Axis_Temp / Axis1_PPD, 2).ToString
                                        'Log.Data.Axis_CPOS(1) = Math.Round(Axis_Temp / ODrive.Axis(1).PPR, 2)
                                        Log.Data.Axis_CPOS(1) = Axis_Temp * ODrive.Axis(1).GearRatio
                                        Log.Data.Axis_RawPosition(1) = Axis_Temp
                                        'CMDDataDrv0.Append("t 1 ")
                                        'CMDDataDrv0.Append((Axis_Temp + ControlRegister(Reg.Axis1_Offset)).ToString)
                                        'CMDDataDrv0.Append(ASCIIlineFeed)
                                        Select Case ODrive.Map_DRV(1)
                                            Case 0 : CMDDataDrv0.Append(ODrive.TCmd(ODrive.Map_OUT(1))) : CMDDataDrv0.Append((Axis_Temp + MotorStallOffset(1) + ControlRegister(Register.Axis1_Offset)).ToString("F4")) : CMDDataDrv0.Append(ASCIIlineFeed) : CMDAvailableDrv0 = True
                                            Case 1 : CMDDataDrv1.Append(ODrive.TCmd(ODrive.Map_OUT(1))) : CMDDataDrv1.Append((Axis_Temp + MotorStallOffset(1) + ControlRegister(Register.Axis1_Offset)).ToString("F4")) : CMDDataDrv1.Append(ASCIIlineFeed) : CMDAvailableDrv1 = True
                                            Case 2 : CMDDataDrv2.Append(ODrive.TCmd(ODrive.Map_OUT(1))) : CMDDataDrv2.Append((Axis_Temp + MotorStallOffset(1) + ControlRegister(Register.Axis1_Offset)).ToString("F4")) : CMDDataDrv2.Append(ASCIIlineFeed) : CMDAvailableDrv2 = True
                                        End Select
                                    Else
                                        Axis_Temp = Axis_Scale(1) * Output_Axis.A1
                                        Log.Data.Axis_CPOS(1) = Axis_Temp * ODrive.Axis(1).GearRatio
                                        Log.Data.Axis_RawPosition(1) = Axis_Temp
                                    End If
                                    'WriteCommand(Reg.Device_ODrive0, CMDDataDrv0.ToString)
                                End If

                                If UseComm(1) Then '--- ODrive 2 (axis 2,3) output
                                    'CMDDataDrv1.Clear()
                                    If ODrive.Motor(2).Ready Then
                                        'Axis_Temp = Math.Truncate(Axis_Scale(2) * Output_Axis.A2)
                                        Axis_Temp = Axis_Scale(2) * Output_Axis.A2
                                        'Axis_Temp = AxisMath.MoveCap(PositionShadow(2), Math.Truncate(Axis2_Scale * Output_Axis.A2), SuddenMove_Limit)
                                        'Axis_Temp = AxisMath.MoveCap(PositionShadow(2), Axis_Temp, SuddenMove_Limit)
                                        If Axis_Temp > ODrive.Axis(2).RangePositive Then Axis_Temp = ODrive.Axis(2).RangePositive : Log.Data.Axis_ERR_INT(2) = 100 'Log.Data.Axis2_ERR_INT = "100:"
                                        If Axis_Temp < ODrive.Axis(2).RangeNegative Then Axis_Temp = ODrive.Axis(2).RangeNegative : Log.Data.Axis_ERR_INT(2) = 101 'Log.Data.Axis2_ERR_INT = "101:"
                                        TotalRevolution(2) += Math.Abs(Axis_Temp - PositionShadow(2))
                                        If Axis_Temp <> PositionShadow(2) Then TotalTime(2) += 25
                                        PositionShadow(2) = Axis_Temp
                                        'TelemetryDataV(0, Telemetry_POS_Target) = Axis_Temp
                                        'Log.Data.Axis2_CPOS = Math.Round(Axis_Temp / Axis2_PPD, 2).ToString
                                        'Log.Data.Axis_CPOS(2) = Math.Round(Axis_Temp / ODrive.Axis(2).PPR, 2)
                                        Log.Data.Axis_CPOS(2) = Axis_Temp * ODrive.Axis(2).GearRatio
                                        Log.Data.Axis_RawPosition(2) = Axis_Temp
                                        'CMDDataDrv1.Append("t 0 ")
                                        'CMDDataDrv1.Append((Axis_Temp + ControlRegister(Reg.Axis2_Offset)).ToString)
                                        'CMDDataDrv1.Append(ASCIIlineFeed)
                                        Select Case ODrive.Map_DRV(2)
                                            Case 0 : CMDDataDrv0.Append(ODrive.TCmd(ODrive.Map_OUT(2))) : CMDDataDrv0.Append((Axis_Temp + MotorStallOffset(2) + ControlRegister(Register.Axis2_Offset)).ToString("F4")) : CMDDataDrv0.Append(ASCIIlineFeed) : CMDAvailableDrv0 = True
                                            Case 1 : CMDDataDrv1.Append(ODrive.TCmd(ODrive.Map_OUT(2))) : CMDDataDrv1.Append((Axis_Temp + MotorStallOffset(2) + ControlRegister(Register.Axis2_Offset)).ToString("F4")) : CMDDataDrv1.Append(ASCIIlineFeed) : CMDAvailableDrv1 = True
                                            Case 2 : CMDDataDrv2.Append(ODrive.TCmd(ODrive.Map_OUT(2))) : CMDDataDrv2.Append((Axis_Temp + MotorStallOffset(2) + ControlRegister(Register.Axis2_Offset)).ToString("F4")) : CMDDataDrv2.Append(ASCIIlineFeed) : CMDAvailableDrv2 = True
                                        End Select
                                    Else
                                        Axis_Temp = Axis_Scale(2) * Output_Axis.A2
                                        Log.Data.Axis_CPOS(2) = Axis_Temp * ODrive.Axis(2).GearRatio
                                        Log.Data.Axis_RawPosition(2) = Axis_Temp
                                    End If
                                    If ODrive.Motor(3).Ready Then
                                        'Axis_Temp = Math.Truncate(Axis_Scale(3) * Output_Axis.A3)
                                        Axis_Temp = Axis_Scale(3) * Output_Axis.A3
                                        'Axis_Temp = AxisMath.MoveCap(PositionShadow(3), Math.Truncate(Axis3_Scale * Output_Axis.A3), SuddenMove_Limit)
                                        'Axis_Temp = AxisMath.MoveCap(PositionShadow(3), Axis_Temp, SuddenMove_Limit)
                                        If Axis_Temp > ODrive.Axis(3).RangePositive Then Axis_Temp = ODrive.Axis(3).RangePositive : Log.Data.Axis_ERR_INT(3) = 100 'Log.Data.Axis3_ERR_INT = "100:"
                                        If Axis_Temp < ODrive.Axis(3).RangeNegative Then Axis_Temp = ODrive.Axis(3).RangeNegative : Log.Data.Axis_ERR_INT(3) = 101 'Log.Data.Axis3_ERR_INT = "101:"
                                        TotalRevolution(3) += Math.Abs(Axis_Temp - PositionShadow(3))
                                        If Axis_Temp <> PositionShadow(3) Then TotalTime(3) += 25
                                        PositionShadow(3) = Axis_Temp
                                        'TelemetryDataV(1, Telemetry_POS_Target) = Axis_Temp
                                        'Log.Data.Axis3_CPOS = Math.Round(Axis_Temp / Axis3_PPD, 2).ToString
                                        'Log.Data.Axis_CPOS(3) = Math.Round(Axis_Temp / ODrive.Axis(3).PPR, 2)
                                        Log.Data.Axis_CPOS(3) = Axis_Temp * ODrive.Axis(3).GearRatio
                                        Log.Data.Axis_RawPosition(3) = Axis_Temp
                                        'CMDDataDrv1.Append("t 1 ")
                                        'CMDDataDrv1.Append((Axis_Temp + ControlRegister(Reg.Axis3_Offset)).ToString)
                                        'CMDDataDrv1.Append(ASCIIlineFeed)
                                        Select Case ODrive.Map_DRV(3)
                                            Case 0 : CMDDataDrv0.Append(ODrive.TCmd(ODrive.Map_OUT(3))) : CMDDataDrv0.Append((Axis_Temp + MotorStallOffset(3) + ControlRegister(Register.Axis3_Offset)).ToString("F4")) : CMDDataDrv0.Append(ASCIIlineFeed) : CMDAvailableDrv0 = True
                                            Case 1 : CMDDataDrv1.Append(ODrive.TCmd(ODrive.Map_OUT(3))) : CMDDataDrv1.Append((Axis_Temp + MotorStallOffset(3) + ControlRegister(Register.Axis3_Offset)).ToString("F4")) : CMDDataDrv1.Append(ASCIIlineFeed) : CMDAvailableDrv1 = True
                                            Case 2 : CMDDataDrv2.Append(ODrive.TCmd(ODrive.Map_OUT(3))) : CMDDataDrv2.Append((Axis_Temp + MotorStallOffset(3) + ControlRegister(Register.Axis3_Offset)).ToString("F4")) : CMDDataDrv2.Append(ASCIIlineFeed) : CMDAvailableDrv2 = True
                                        End Select
                                    Else
                                        Axis_Temp = Axis_Scale(3) * Output_Axis.A3
                                        Log.Data.Axis_CPOS(3) = Axis_Temp * ODrive.Axis(3).GearRatio
                                        Log.Data.Axis_RawPosition(3) = Axis_Temp
                                    End If
                                End If

                                If UseComm(2) Then '--- ODrive 3 (axis 4,5) output
                                    If ODrive.Motor(4).Ready Then
                                        'Axis_Temp = Math.Truncate(Axis_Scale(4) * Output_Axis.A4)
                                        Axis_Temp = Axis_Scale(4) * Output_Axis.A4
                                        'Axis_Temp = AxisMath.MoveCap(PositionShadow(4), Axis_Temp, SuddenMove_Limit)
                                        If Axis_Temp > ODrive.Axis(4).RangePositive Then Axis_Temp = ODrive.Axis(4).RangePositive : Log.Data.Axis_ERR_INT(4) = 100
                                        If Axis_Temp < ODrive.Axis(4).RangeNegative Then Axis_Temp = ODrive.Axis(4).RangeNegative : Log.Data.Axis_ERR_INT(4) = 101
                                        TotalRevolution(4) += Math.Abs(Axis_Temp - PositionShadow(4))
                                        If Axis_Temp <> PositionShadow(4) Then TotalTime(4) += 25
                                        PositionShadow(4) = Axis_Temp
                                        'Log.Data.Axis_CPOS(4) = Math.Round(Axis_Temp / ODrive.Axis(4).PPR, 2)
                                        Log.Data.Axis_CPOS(4) = Axis_Temp * ODrive.Axis(4).GearRatio
                                        Log.Data.Axis_RawPosition(4) = Axis_Temp
                                        Select Case ODrive.Map_DRV(4)
                                            Case 0 : CMDDataDrv0.Append(ODrive.TCmd(ODrive.Map_OUT(4))) : CMDDataDrv0.Append((Axis_Temp + MotorStallOffset(4) + ControlRegister(Register.Axis4_Offset)).ToString("F4")) : CMDDataDrv0.Append(ASCIIlineFeed) : CMDAvailableDrv0 = True
                                            Case 1 : CMDDataDrv1.Append(ODrive.TCmd(ODrive.Map_OUT(4))) : CMDDataDrv1.Append((Axis_Temp + MotorStallOffset(4) + ControlRegister(Register.Axis4_Offset)).ToString("F4")) : CMDDataDrv1.Append(ASCIIlineFeed) : CMDAvailableDrv1 = True
                                            Case 2 : CMDDataDrv2.Append(ODrive.TCmd(ODrive.Map_OUT(4))) : CMDDataDrv2.Append((Axis_Temp + MotorStallOffset(4) + ControlRegister(Register.Axis4_Offset)).ToString("F4")) : CMDDataDrv2.Append(ASCIIlineFeed) : CMDAvailableDrv2 = True
                                        End Select
                                    Else
                                        Axis_Temp = Axis_Scale(4) * Output_Axis.A4
                                        Log.Data.Axis_CPOS(4) = Axis_Temp * ODrive.Axis(4).GearRatio
                                        Log.Data.Axis_RawPosition(4) = Axis_Temp
                                    End If
                                    If ODrive.Motor(5).Ready Then
                                        'Axis_Temp = Math.Truncate(Axis_Scale(5) * Output_Axis.A5)
                                        Axis_Temp = Axis_Scale(5) * Output_Axis.A5
                                        'Axis_Temp = AxisMath.MoveCap(PositionShadow(5), Axis_Temp, SuddenMove_Limit)
                                        If Axis_Temp > ODrive.Axis(5).RangePositive Then Axis_Temp = ODrive.Axis(5).RangePositive : Log.Data.Axis_ERR_INT(5) = 100
                                        If Axis_Temp < ODrive.Axis(5).RangeNegative Then Axis_Temp = ODrive.Axis(5).RangeNegative : Log.Data.Axis_ERR_INT(5) = 101
                                        TotalRevolution(5) += Math.Abs(Axis_Temp - PositionShadow(5))
                                        If Axis_Temp <> PositionShadow(5) Then TotalTime(5) += 25
                                        PositionShadow(5) = Axis_Temp
                                        'Log.Data.Axis_CPOS(5) = Math.Round(Axis_Temp / ODrive.Axis(5).PPR, 2)
                                        Log.Data.Axis_CPOS(5) = Axis_Temp * ODrive.Axis(5).GearRatio
                                        Log.Data.Axis_RawPosition(5) = Axis_Temp
                                        Select Case ODrive.Map_DRV(5)
                                            Case 0 : CMDDataDrv0.Append(ODrive.TCmd(ODrive.Map_OUT(5))) : CMDDataDrv0.Append((Axis_Temp + MotorStallOffset(5) + ControlRegister(Register.Axis5_Offset)).ToString("F4")) : CMDDataDrv0.Append(ASCIIlineFeed) : CMDAvailableDrv0 = True
                                            Case 1 : CMDDataDrv1.Append(ODrive.TCmd(ODrive.Map_OUT(5))) : CMDDataDrv1.Append((Axis_Temp + MotorStallOffset(5) + ControlRegister(Register.Axis5_Offset)).ToString("F4")) : CMDDataDrv1.Append(ASCIIlineFeed) : CMDAvailableDrv1 = True
                                            Case 2 : CMDDataDrv2.Append(ODrive.TCmd(ODrive.Map_OUT(5))) : CMDDataDrv2.Append((Axis_Temp + MotorStallOffset(5) + ControlRegister(Register.Axis5_Offset)).ToString("F4")) : CMDDataDrv2.Append(ASCIIlineFeed) : CMDAvailableDrv2 = True
                                        End Select
                                    Else
                                        Axis_Temp = Axis_Scale(5) * Output_Axis.A5
                                        Log.Data.Axis_CPOS(5) = Axis_Temp * ODrive.Axis(5).GearRatio
                                        Log.Data.Axis_RawPosition(5) = Axis_Temp
                                    End If
                                End If

                                'Debug.WriteLine("Out " & Output_Axis.A0.ToString & " " & Output_Axis.A1.ToString & " " & Output_Axis.A2.ToString & " " & Output_Axis.A3.ToString & " " & Output_Axis.A4.ToString & " " & Output_Axis.A5.ToString & " ")
                                'Debug.WriteLine("Sca " & Axis_Scale(0).ToString & " " & Axis_Scale(1).ToString & " " & Axis_Scale(2).ToString & " " & Axis_Scale(3).ToString & " " & Axis_Scale(4).ToString & " " & Axis_Scale(5).ToString & " ")
                                'Debug.WriteLine("-")
                                If CMDAvailableDrv0 Then WriteCommand(Reg.Device_ODrive0, CMDDataDrv0.ToString)
                                If CMDAvailableDrv1 Then WriteCommand(Reg.Device_ODrive1, CMDDataDrv1.ToString)
                                If CMDAvailableDrv2 Then WriteCommand(Reg.Device_ODrive2, CMDDataDrv2.ToString)

                                'If UseComm(2) Then '--- fan ESC output ---
                                '    CMDDataOut.Clear()
                                '    If ODrive.Motor(7).Ready Then
                                '        CMDDataOut.Append("F")
                                '        Axis_Temp = Math.Truncate(Axis5_Scale * Output_Axis.A5)
                                '        If Axis_Temp > Axis5_PRange Then Axis_Temp = Axis5_PRange
                                '        If Axis_Temp < 1 Then Axis_Temp = 1
                                '        CMDDataOut.Append(Math.Truncate(Axis_Temp).ToString)
                                '        CMDDataOut.Append(Math.Truncate(Axis_Temp).ToString)
                                '        CMDDataOut.Append("1111")
                                '    End If
                                '    'WriteCommand(Reg.Device_SystemIO, CMDDataOut.ToString)
                                'End If

                                CMDDataOut.Clear()
                            Else
                                CMDRes.Append("invalid command")
                            End If
                        Case 990 'SYS.DEVICE.LIST
                            '--- Warning this will clear UDP output buffer ---
                            '---  any respond that not send yet will lost  ---
                            'CMDRes.Clear() ' <----
                            CMDRes.Append("DEVICE.LIST=")
                            For devloop As Integer = 0 To UARTNameID.Length - 1
                                If UARTNameID(devloop) <> "" Then
                                    CMDRes.Append(" ")
                                    CMDRes.Append(UARTNameID(devloop))
                                    CMDRes.Append(" ")
                                End If
                            Next
                        Case 999 'SYS.SHUTDOWN

                        Case Else
                            CMDValid = False
                    End Select
                End If
                If Not CMDValid Then
                    CMDRes.Append(" invalid command ")
                    CMDRes.Append(SYSMode.ToString)
                    CMDRes.Append("(")
                    CMDRes.Append(CMDQuery(i))
                    CMDRes.Append(")")
                    Debug.WriteLine(CMDRes.ToString)
                End If
            Next

        End If
        'if nothing to report just say "OK"
        'If CMDRes.Length = CIDLenght Then
        'CMDRes.Append("OK")
        'End If
        If CMDRes.Length > CIDLenght Then
            'TXBuffer = Ascii.GetBytes(CMDRes.ToString)
            'If CMDRes.Length > 0 Then
            '    NETTransmitter.SendAsync(TXBuffer, CMDRes.Length, PCEndpoint)
            '    NETTransmitter.SendAsync(TXBuffer, CMDRes.Length, DashboardEndpoint)
            'End If
            WriteRespondBinary(System.Text.Encoding.ASCII.GetBytes(CMDRes.ToString), RespondTo.PC)
            WriteRespondBinary(System.Text.Encoding.ASCII.GetBytes(CMDRes.ToString), RespondTo.Dashboard)
        End If
        CMDRes.Clear()
        CMDBUSY = False

        'If RegisterWriteEvent Then
        '  If ControlRegister(Reg.SetAsOffset) = Reg.OFFSET_USE Then
        '    ControlRegister(Reg.SetAsOffset) = Reg.OFFSET_NONE
        '  End If
        'End If

        'performanceTimer_end = System.DateTime.Now.Ticks
        'Debug.WriteLine("CMD Time : " & Math.Abs(performanceTimer_start - performanceTimer_end).ToString)

        Return COMOK
    End Function

    Private Sub Motor_Parking()
        Dim CMDString As New System.Text.StringBuilder
        Dim MotorOutput As Single = 100.0
        Dim OutputScale As Single = 1
        Dim POSZero As Integer = 0
        Const MotorHalfTurn As Integer = 1024
        Dim SetResult As Integer = 0
        SysEvent("platfrom parking ....")
        MotorOutput = Convert.ToSingle(AxisMath.getUsage)
        ControlRegister(Register.MotorState_Parking) = 1
        Task.Delay(100).Wait()

        For i As Integer = 0 To 5
            If ODrive.Map_DRV(i) >= 0 And ODrive.Map_OUT(i) >= 0 Then
                If ODrive.Motor(i).Enable Then
                    SetResult = 0
                    If ODrive.PositionUnit = ODrive.Unit.Pulse Then
                        SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(ODrive.Axis(i).ParkProfile).Velocity_Limit, ODrive.Axis(i).PPR), True)
                        SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(ODrive.Axis(i).ParkProfile).Trajectory_Velocity_Limit, ODrive.Axis(i).PPR), True)
                    End If
                    If ODrive.PositionUnit = ODrive.Unit.Turn Then
                        SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.VEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).ParkProfile).Velocity_Limit, True)
                        SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).ParkProfile).Trajectory_Velocity_Limit, True)
                    End If
                    SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_ACCEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).ParkProfile).Acceleration_Limit, True)
                    SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_DECEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).ParkProfile).Deacceleration_Limit, True)
                    MotorEvent(ODrive.Map_DRV(i), ODrive.Map_OUT(i), " set velocity:" & ODrive.MotorPreset(ODrive.Axis(i).ParkProfile).Velocity_Limit.ToString & " - " & ODrive.MotorPreset(ODrive.Axis(i).ParkProfile).Trajectory_Velocity_Limit.ToString)
                    If SetResult > 0 Then
                        Debug.WriteLine("Axis " & i.ToString & " parameter set error")
                        ODrive.Motor(i).Enable = False
                    End If
                End If
            End If
        Next

        Task.Delay(100).Wait()
        Do While ControlRegister(Register.MotorState_Parking) > 0 And MotorOutput > 0.0
            If MotorOutput < 0.0 Then MotorOutput = 0.0
            POSZero = 0
            For i As Integer = 0 To 5
                If Math.Abs(Log.Data.Axis_RawPosition(i)) <= MotorHalfTurn Then POSZero += 1
            Next
            If POSZero = 6 Then MotorOutput = 0.0

            AxisMath.setUsage(Math.Truncate(MotorOutput))
            Task.Delay(25).Wait()
            CMDString.Clear()
            CMDString.Append("[0]sys.pos(")
            CMDString.Append(Log.Data.DOF0_POS)
            CMDString.Append(",")
            CMDString.Append(Log.Data.DOF1_POS)
            CMDString.Append(",")
            CMDString.Append(Log.Data.DOF2_POS)
            CMDString.Append(",")
            CMDString.Append(Log.Data.DOF3_POS)
            CMDString.Append(",")
            CMDString.Append(Log.Data.DOF4_POS)
            CMDString.Append(",")
            CMDString.Append(Log.Data.DOF5_POS)
            CMDString.Append(")")
            CommandProcessor(CMDString.ToString)
            MotorOutput -= OutputScale

        Loop
        SysEvent("in parking position .... safety delay 5 sec")
        Task.Delay(5000).Wait()

        For i As Integer = 0 To 5
            If ODrive.Map_DRV(i) >= 0 And ODrive.Map_OUT(i) >= 0 Then
                If ODrive.Motor(i).Enable Then
                    SetResult = 0
                    If ODrive.PositionUnit = ODrive.Unit.Pulse Then
                        SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(ODrive.Axis(i).RunProfile).Velocity_Limit, ODrive.Axis(i).PPR), True)
                        SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(ODrive.Axis(i).RunProfile).Trajectory_Velocity_Limit, ODrive.Axis(i).PPR), True)
                    End If
                    If ODrive.PositionUnit = ODrive.Unit.Turn Then
                        SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.VEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).RunProfile).Velocity_Limit, True)
                        SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).RunProfile).Trajectory_Velocity_Limit, True)
                    End If
                    SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_ACCEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).RunProfile).Acceleration_Limit, True)
                    SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_DECEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).RunProfile).Deacceleration_Limit, True)
                    MotorEvent(ODrive.Map_DRV(i), ODrive.Map_OUT(i), " set velocity " & ODrive.MotorPreset(ODrive.Axis(i).RunProfile).Velocity_Limit.ToString & " - " & ODrive.MotorPreset(ODrive.Axis(i).ParkProfile).Trajectory_Velocity_Limit.ToString)
                    If SetResult > 0 Then
                        Debug.WriteLine("Axis " & i.ToString & " parameter set error")
                        ODrive.Motor(i).Enable = False
                    End If
                End If
            End If
        Next

        Task.Delay(100).Wait()
        ControlRegister(Register.MotorState_Parking) = 0
        AxisMath.setUsage(0)
        CMDString.Clear()
        SysEvent("platfrom parking finish")
    End Sub

    Private Sub onCanceled(sender As IBackgroundTaskInstance, reason As BackgroundTaskCancellationReason)
        TaskCancle = True
        For i As Integer = 0 To GPIOAvailablePin.Length
            If GPIOPIN(i) IsNot Nothing Then GPIOPIN(i).Dispose()
            Debug.WriteLine("GPIO " & i.ToString & " Close")
        Next
        For i As Integer = 0 To TotalUART
            If UseComm(i) And UARTDevice(i) IsNot Nothing Then
                UARTDevice(i).Dispose()
                CommReader(i).Dispose()
                Debug.WriteLine("UART-RX " & i.ToString & " Close")
            End If
        Next
        For i As Integer = 0 To TotalUART
            If CommWriter(i) IsNot Nothing Then
                CommWriter(i).Dispose()
                Debug.WriteLine("UART-TX " & i.ToString & " Close")
            End If
        Next
        If I2CDevice IsNot Nothing Then I2CDevice.Dispose()
        If BGTask IsNot Nothing Then BGTask.Complete()
    End Sub

    Private Async Function CommSET(ByVal UseComm() As Boolean, CommNo As Integer, CommID As String, Baud As Integer, ReadTimeout As Integer, FixedLenght As Boolean) As Task(Of Boolean)
        If EmergencySTOP Then Return False
        If UseComm(CommNo) Then
            UseComm(CommNo) = False
            Debug.WriteLine("CLOSE : " & CommNo.ToString)
            UARTDevice(CommNo).Dispose()
            CommReader(CommNo).Dispose()
        End If
        Try
            Debug.WriteLine("CONNECT : " & CommNo.ToString & "[" & Baud.ToString & "] : " & CommID)
            UARTDevice(CommNo) = Await Windows.Devices.SerialCommunication.SerialDevice.FromIdAsync(CommID)
            UARTDevice(CommNo).WriteTimeout = TimeSpan.FromMilliseconds(1000)
            UARTDevice(CommNo).ReadTimeout = TimeSpan.FromMilliseconds(ReadTimeout)
            UARTDevice(CommNo).BaudRate = Baud
            UARTDevice(CommNo).Parity = Windows.Devices.SerialCommunication.SerialParity.None
            UARTDevice(CommNo).StopBits = Windows.Devices.SerialCommunication.SerialStopBitCount.One
            UARTDevice(CommNo).DataBits = 8
            UARTDevice(CommNo).Handshake = Windows.Devices.SerialCommunication.SerialHandshake.None
            CommReader(CommNo) = New Windows.Storage.Streams.DataReader(UARTDevice(CommNo).InputStream)
            'If FixedLenght Then
            'CommReader(CommNo).InputStreamOptions = Windows.Storage.Streams.InputStreamOptions.None
            'Else
            CommReader(CommNo).InputStreamOptions = Windows.Storage.Streams.InputStreamOptions.Partial
            'End If
            CommWriter(CommNo) = New Windows.Storage.Streams.DataWriter(UARTDevice(CommNo).OutputStream)
            CommRX(CommNo) = ""
            UseComm(CommNo) = True

        Catch ex As Exception
            Debug.WriteLine("UART ERROR : " + ex.Message)
            Debug.WriteLine("UART REF : " + CommID)
            UseComm(CommNo) = False
        End Try
        Return True
    End Function

    Public Function GetIPAddress() As String 'System.Net.IPAddress
        Dim IpAddress As List(Of String) = New List(Of String)()
        Dim Hosts = Windows.Networking.Connectivity.NetworkInformation.GetHostNames().ToList()

        For Each Host In Hosts
            Dim IP As String = Host.DisplayName
            IpAddress.Add(IP)
        Next

        Dim address As String = System.Net.IPAddress.Parse(IpAddress.Last()).ToString
        Return address
    End Function

    Private Function Drive_Connect(DrvID As Integer) As Boolean
        Dim DriveOK As Boolean = False
        Dim DeviceName As String = ""
        Dim DeviceBPS As Integer = 0
        Select Case DrvID
            Case 0
                DeviceName = Drive0_DeviceName
                DeviceBPS = Drive0_Baud
            Case 1
                DeviceName = Drive1_DeviceName
                DeviceBPS = Drive1_Baud
            Case 2
                DeviceName = Drive2_DeviceName
                DeviceBPS = Drive2_Baud
            Case 3
                DeviceName = Drive3_DeviceName
                DeviceBPS = Drive3_Baud
            Case Else
                SysEvent("Invalid DriveID " & DrvID.ToString)
                Return False
        End Select
        If DeviceName.Length > 0 Then
            'Debug.WriteLine("Init Device : " & DeviceName)
            If DeviceExist(DeviceName) Then
                If CommandProcessor("[0]DRIVE(" & DrvID.ToString & ").USE(" & DeviceName & "," & DeviceBPS.ToString & ")") Then
                    DriveOK = True
                    Debug.WriteLine("Drive " & DrvID.ToString & " connected")
                    Task.Delay(100).Wait()
                    CommandProcessor(ODrive.WriteCommand(DrvID, 0, ODrive.REQUEST_STATE, ODrive.AXIS_STATE_IDLE))
                    Task.Delay(100).Wait()
                    CommandProcessor(ODrive.WriteCommand(DrvID, 1, ODrive.REQUEST_STATE, ODrive.AXIS_STATE_IDLE))
                    Task.Delay(100).Wait()
                End If
            Else
                SysEvent("Drive " & DrvID.ToString & " notfound")
                Return False
            End If
        End If
        Return DriveOK
    End Function

    Private Function System_Connect() As Boolean
        Dim DriveOK As Boolean = False
        If Local_DeviceName.Length > 0 Then
            If DeviceExist(Local_DeviceName) Then
                If CommandProcessor("[0]DRIVE(4).USE(" & Local_DeviceName & "," & Local_Baud.ToString & ")") Then
                    DriveOK = True
                    SysEvent("System PCB connected")
                End If
            Else
                SysEvent("System PCB notfound")
            End If
        Else
            SysEvent("System PCB not config")
        End If
        Return DriveOK
    End Function

    Private Function Display_Connect() As Boolean
        Dim DriveOK As Boolean = False
        If Display_DeviceName.Length > 0 Then
            If DeviceExist(Display_DeviceName) Then
                If CommandProcessor("[0]DRIVE(5).USE(" & Display_DeviceName & "," & Display_Baud.ToString & ")") Then
                    DriveOK = True
                    SysEvent("Display connected")
                Else
                    SysEvent("Display connection error")
                End If
            Else
                SysEvent("Display not found")
            End If
        Else
            SysEvent("Display not config")
        End If
        Return DriveOK
    End Function

    Private Function Control_Connect() As Boolean
        Dim DriveOK As Boolean = False
        If Control_DeviceName.Length > 0 Then
            If DeviceExist(Control_DeviceName) Then
                If CommandProcessor("[0]DRIVE(6).USE(" & Control_DeviceName & "," & Control0_Baud.ToString & ")") Then
                    DriveOK = True
                    SysEvent("Control connected")
                Else
                    SysEvent("Control connection error")
                End If
            Else
                SysEvent("Control not found")
            End If
        Else
            SysEvent("Control not config")
        End If
        Return DriveOK
    End Function

    Private Function Home_Direct(DrvID As Integer, MotorID As Integer, PresetID As Integer, Offset As Integer) As Boolean

        Const MaxRPM As Single = 3000
        'Const State_Wait As Integer = 250 ' m/sec
        Dim Result As Boolean = False
        Dim CurrentPOS As Integer = 0
        Dim CenterPOS As Integer = 0
        Dim XCnt As Integer = 0
        Dim Vel_Limit As Single = Math.Truncate((MaxRPM / 60.0) * 2048.0)
        Dim ENCODER_READY As Boolean = False
        Dim MotorResult As Integer = 0
        Dim SetResult As Integer = 0
        Dim MotorNo As Integer = 0
        Dim DriveError As Integer = 0
        Dim TimeStamp As Integer = System.Environment.TickCount
        Dim xCount As Integer = 0
        Dim STATE_WAIT As Integer = OPConfig.Delay_StateChange
        Dim CMD_WAIT As Integer = OPConfig.Delay_ParameterSet

        Debug.WriteLine("Center DRV:" & DrvID.ToString & " Motor:" & MotorID.ToString & " MODE:direct")

        If DrvID >= 0 And MotorID >= 0 Then
            For i As Integer = 0 To 3
                If DrvID = ODrive.Map_DRV(i) And MotorID = ODrive.Map_OUT(i) Then
                    MotorNo = i
                    Exit For
                End If
            Next
        End If
        Select Case MotorNo
            Case 0 : ControlRegister(Register.Axis0_Offset) = 0
            Case 1 : ControlRegister(Register.Axis1_Offset) = 0
            Case 2 : ControlRegister(Register.Axis2_Offset) = 0
            Case 3 : ControlRegister(Register.Axis3_Offset) = 0
            Case 4 : ControlRegister(Register.Axis4_Offset) = 0
            Case 5 : ControlRegister(Register.Axis5_Offset) = 0
            Case 6 : ControlRegister(Register.Axis6_Offset) = 0
            Case 7 : ControlRegister(Register.Axis7_Offset) = 0
            Case Else
                Debug.WriteLine("ERROR:Invalid Mapping [" & MotorNo.ToString & "] DRV:" & DrvID.ToString & " OUT:" & MotorID.ToString)
                Return False
        End Select
        If MotorNo < 0 Then
            Debug.WriteLine("ERROR:Invalid Mapping [" & MotorNo.ToString & "] DRV:" & DrvID.ToString & " OUT:" & MotorID.ToString)
            Return False
        End If

        DriveError = Get_Error(DrvID, MotorID)
        If DriveError > 0 Then
            Debug.WriteLine("ERROR:" & Motor(MotorNo).Text)
            Return False
        End If
        If DriveError < 0 Then
            Debug.WriteLine("ERROR:Drive Offline (" & DriveError & ")")
            Return False
        End If

        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
        If MotorResult > 0 Then
            If Get_Error(DrvID, MotorID) > 0 Then MotorEvent(DrvID, MotorID, ODrive.Motor(MotorNo).ErrText)
            If Get_Error(DrvID, MotorID) < 0 Then MotorEvent(DrvID, MotorID, "drive offline")
            If Get_Error(DrvID, MotorID) = 0 Then MotorEvent(DrvID, MotorID, "unknown")
            Return False
        End If

        'check if encoder is ready
        MotorEvent(DrvID, MotorID, "encoder check")
        IORegister(DrvID, Reg.IO_HEAD) = 0
        For i As Integer = 0 To 5
            CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_IS_READY))
            Task.Delay(100).Wait()
            If IORegister(DrvID, Reg.IO_HEAD) > 0 Then
                ENCODER_READY = True
                Exit For
            End If
        Next

        If ENCODER_READY Then
            Task.Delay(STATE_WAIT).Wait()
            MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_CLOSED_LOOP_CONTROL)
            If MotorResult > 0 Then
                If Get_Error(DrvID, MotorID) > 0 Then
                    Debug.WriteLine(ODrive.Motor(MotorNo).Text)
                End If
            End If
            MotorEvent(DrvID, MotorID, "move center")
            Task.Delay(250).Wait()
            CenterPOS = Offset
            xCount = 0
            CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & CenterPOS.ToString & ")")
            For i As Integer = 0 To 100
                CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
                Task.Delay(100).Wait()
                CurrentPOS = Convert.ToInt32(IORegister(DrvID, Reg.IO_HEAD))
                If Math.Abs(CurrentPOS - CenterPOS) < 100 Then
                    Task.Delay(250).Wait()
                    xCount += 1
                    If xCount > 4 Then Exit For
                End If
            Next
            MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)

            Select Case MotorNo
                Case 0 : ControlRegister(Register.Axis0_Offset) = CenterPOS
                Case 1 : ControlRegister(Register.Axis1_Offset) = CenterPOS
                Case 2 : ControlRegister(Register.Axis2_Offset) = CenterPOS
                Case 3 : ControlRegister(Register.Axis3_Offset) = CenterPOS
                Case 4 : ControlRegister(Register.Axis4_Offset) = CenterPOS
                Case 5 : ControlRegister(Register.Axis5_Offset) = CenterPOS
                Case 6 : ControlRegister(Register.Axis6_Offset) = CenterPOS
                Case 7 : ControlRegister(Register.Axis7_Offset) = CenterPOS
            End Select
            'If DrvID = ODrive.Map_DRV(0) And MotorID = ODrive.Map_OUT(0) Then ControlRegister(Register.Axis0_Offset) = CenterPOS
            'If DrvID = ODrive.Map_DRV(1) And MotorID = ODrive.Map_OUT(1) Then ControlRegister(Register.Axis1_Offset) = CenterPOS
            'If DrvID = ODrive.Map_DRV(2) And MotorID = ODrive.Map_OUT(2) Then ControlRegister(Register.Axis2_Offset) = CenterPOS
            'If DrvID = ODrive.Map_DRV(3) And MotorID = ODrive.Map_OUT(3) Then ControlRegister(Register.Axis3_Offset) = CenterPOS
            'If DrvID = ODrive.Map_DRV(4) And MotorID = ODrive.Map_OUT(4) Then ControlRegister(Register.Axis4_Offset) = CenterPOS
            'If DrvID = ODrive.Map_DRV(5) And MotorID = ODrive.Map_OUT(5) Then ControlRegister(Register.Axis5_Offset) = CenterPOS
            'If DrvID = ODrive.Map_DRV(6) And MotorID = ODrive.Map_OUT(6) Then ControlRegister(Register.Axis6_Offset) = CenterPOS
            'If DrvID = ODrive.Map_DRV(7) And MotorID = ODrive.Map_OUT(7) Then ControlRegister(Register.Axis7_Offset) = CenterPOS

            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_CURRENT, 10.0, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIM, ODrive.MotorPreset(PresetID).Current_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIMIT_TOLERANCE, ODrive.MotorPreset(PresetID).Current_Limit_Tolerance, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.REQUESTED_CURRENT_RANGE, ODrive.MotorPreset(PresetID).Current_Range, True)
            ''SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_CONTROL_BANDWIDTH, 500.0, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.SlowPreset(PresetID).Velocity_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.SlowPreset(PresetID).Velocity_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.SlowPreset(PresetID).Acceleration_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.SlowPreset(PresetID).Deacceleration_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(PresetID).Position_Gain, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(PresetID).Velocity_Gain, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(PresetID).Velocity_Integrator_Gain, True)

            If SetResult + MotorResult = 0 Then
                Result = True
            Else
                Result = False
            End If

        End If

        If MotorResult > 0 Then
            Dim TmpErrStr As String = ""
            If Get_Error(DrvID, MotorID) > 0 Then
                MotorEvent(DrvID, MotorID, ODrive.Motor(MotorNo).ErrText)
            Else
                MotorEvent(DrvID, MotorID, "DRV ERROR")
            End If
            Result = False
        End If

        If Result Then MotorEvent(DrvID, MotorID, "ready")


        Debug.WriteLine("finish ( T = " & (System.Environment.TickCount - TimeStamp).ToString & " )")

        Return Result

    End Function

    Private Function Home_Circle(DrvID As Integer, MotorID As Integer, HomeLimit As Integer, HomeTotalance As Integer, HomeTimeout As Integer, IndexTimeout As Integer, PresetCAL As Integer, PresetRUN As Integer, IndexOnly As Boolean) As Boolean
        Dim DistantLimit_P As Integer = HomeLimit
        Dim DistantLimit_M As Integer = DistantLimit_P * -1
        Const HIT_Wait As Integer = 10
        Const HIT_Range As Integer = 30
        Dim HitDetect As Integer = 0
        Dim Result As Boolean = False
        Dim CurrentPOS As Integer = 0
        Dim ShadowPOS As Integer = 999999
        Dim InitPOS As Integer = 0
        Dim OffsetPlus As Integer = 0
        Dim OffsetMinus As Integer = 0
        Dim CenterPOS As Integer = 0
        Dim XCnt As Integer = 0
        'Dim Vel_Limit As Single = Math.Truncate((MaxRPM / 60.0) * 2048.0)
        Dim ENCODER_READY As Boolean = False
        Dim MotorResult As Integer = 0
        Dim SetResult As Integer = 0
        Dim MotorNo As Integer = -1
        Dim DriveError As Integer = 0
        Dim TimeStamp As Integer = System.Environment.TickCount
        Dim LocalHomeStartTime As Integer = 0
        Dim LocalHomeTimeout As Integer = HomeTimeout * 1000
        Dim LocalIndexTimeout As Integer = IndexTimeout * 1000
        Dim TimeoutDetected As Boolean = False
        Dim STATE_WAIT As Integer = OPConfig.Delay_StateChange
        Dim CMD_WAIT As Integer = OPConfig.Delay_ParameterSet

        Debug.WriteLine("Homing DRV:" & DrvID.ToString & " Motor:" & MotorID.ToString & " MODE:linear")
        Debug.WriteLine("Home Totalance (PPR):" & HomeTotalance.ToString)

        If DrvID >= 0 And MotorID >= 0 Then
            For i As Integer = 0 To 3
                If DrvID = ODrive.Map_DRV(i) And MotorID = ODrive.Map_OUT(i) Then
                    MotorNo = i
                    Exit For
                End If
            Next
        End If
        Select Case MotorNo
            Case 0 : ControlRegister(Register.Axis0_Offset) = 0
            Case 1 : ControlRegister(Register.Axis1_Offset) = 0
            Case 2 : ControlRegister(Register.Axis2_Offset) = 0
            Case 3 : ControlRegister(Register.Axis3_Offset) = 0
            Case 4 : ControlRegister(Register.Axis4_Offset) = 0
            Case 5 : ControlRegister(Register.Axis5_Offset) = 0
            Case 6 : ControlRegister(Register.Axis6_Offset) = 0
            Case 7 : ControlRegister(Register.Axis7_Offset) = 0
            Case Else
                Debug.WriteLine("ERROR: Invalid Mapping [" & MotorNo.ToString & "] DRV:" & DrvID.ToString & " OUT:" & MotorID.ToString)
                Return False
        End Select
        If MotorNo < 0 Then
            Debug.WriteLine("ERROR: Invalid Mapping [" & MotorNo.ToString & "] DRV:" & DrvID.ToString & " OUT:" & MotorID.ToString)
            Return False
        End If
        DriveError = Get_Error(DrvID, MotorID)
        If DriveError > 0 Then
            Debug.WriteLine("ERROR: " & Motor(MotorNo).Text)
            Return False
        End If
        If DriveError < 0 Then
            Debug.WriteLine("ERROR :Drive Offline (" & DriveError & ")")
            Return False
        End If

        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)

        'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_CURRENT, ODrive.MotorPreset(PresetCAL).Calibration_Current, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIM, ODrive.MotorPreset(PresetCAL).Current_Limit, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIMIT_TOLERANCE, ODrive.MotorPreset(PresetCAL).Current_Limit_Tolerance, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.REQUESTED_CURRENT_RANGE, ODrive.MotorPreset(PresetCAL).Current_Range, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_CONTROL_BANDWIDTH, ODrive.MotorPreset(PresetCAL).Current_Control_Bandwidth, True)

        If ODrive.PositionUnit = ODrive.Unit.Pulse Then
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetCAL).Velocity_Limit, ODrive.Axis(MotorNo).PPR), True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetCAL).Trajectory_Velocity_Limit, ODrive.Axis(MotorNo).PPR), True)
        End If
        If ODrive.PositionUnit = ODrive.Unit.Turn Then
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.MotorPreset(PresetCAL).Velocity_Limit, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(PresetCAL).Trajectory_Velocity_Limit, True)
        End If

        SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.MotorPreset(PresetCAL).Acceleration_Limit, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.MotorPreset(PresetCAL).Deacceleration_Limit, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(PresetCAL).Position_Gain, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(PresetCAL).Velocity_Gain, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(PresetCAL).Velocity_Integrator_Gain, True)

        SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, ODrive.MotorPreset(PresetCAL).Calibration_Velocity, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, ODrive.MotorPreset(PresetCAL).Calibration_Acceleration, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, ODrive.MotorPreset(PresetCAL).Calibration_Ramp, True)
        If MotorResult > 0 Then
            If Get_Error(DrvID, MotorID) > 0 Then MotorEvent(DrvID, MotorID, ODrive.Motor(MotorNo).ErrText)
            If Get_Error(DrvID, MotorID) < 0 Then MotorEvent(DrvID, MotorID, "drive offline")
            If Get_Error(DrvID, MotorID) = 0 Then MotorEvent(DrvID, MotorID, "unknown")
            Return False
        End If

        'check if encoder is ready
        MotorEvent(DrvID, MotorID, "encoder check")
        IORegister(DrvID, Reg.IO_HEAD) = 0
        For i As Integer = 0 To 5
            CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_IS_READY))
            Task.Delay(100).Wait()
            If IORegister(DrvID, Reg.IO_HEAD) > 0 Then
                ENCODER_READY = True
                Exit For
            End If
        Next

        'index search
        If Not ENCODER_READY Then
            MotorEvent(DrvID, MotorID, "index")
            IORegister(DrvID, Reg.IO_HEAD) = 0
            MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_ENCODER_INDEX_SEARCH)
            Task.Delay(100).Wait()
            IORegister(DrvID, Reg.IO_HEAD) = 0
            For i As Integer = 0 To 100
                CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_IS_READY))
                Task.Delay(100).Wait()
                If IORegister(DrvID, Reg.IO_HEAD) > 0 Then
                    IORegister(DrvID, Reg.IO_HEAD) = 0
                    For j As Integer = 0 To 100
                        CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.CURRENT_STATE))
                        Task.Delay(100).Wait()
                        If IORegister(DrvID, Reg.IO_HEAD) = ODrive.AXIS_STATE_IDLE Then
                            ENCODER_READY = True
                            Exit For
                        End If
                    Next
                    Exit For
                End If
            Next
        End If
        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
        If MotorResult > 0 Then
            Select Case Get_Error(DrvID, MotorID)
                Case > 0 : MotorEvent(DrvID, MotorID, ODrive.Motor(MotorNo).ErrText)
                Case < 0 : MotorEvent(DrvID, MotorID, "ERROR: drive offline")
                Case = 0 : MotorEvent(DrvID, MotorID, "ERROR: unknown")
            End Select
            Return False
        End If

        'reverse index search
        If Not ENCODER_READY Then
            MotorEvent(DrvID, MotorID, "reverse index")
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, ODrive.MotorPreset(PresetCAL).Calibration_Velocity * -1.0, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, ODrive.MotorPreset(PresetCAL).Calibration_Acceleration * -1.0, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, ODrive.MotorPreset(PresetCAL).Calibration_Ramp * -1.0, True)
            If SetResult = 0 Then
                IORegister(DrvID, Reg.IO_HEAD) = 0
                MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_ENCODER_INDEX_SEARCH)
                Task.Delay(100).Wait()
                IORegister(DrvID, Reg.IO_HEAD) = 0
                For i As Integer = 1 To 100
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_IS_READY))
                    Task.Delay(100).Wait()
                    If IORegister(DrvID, Reg.IO_HEAD) > 0 Then
                        IORegister(DrvID, Reg.IO_HEAD) = 0
                        For j As Integer = 0 To 10
                            CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.CURRENT_STATE))
                            Task.Delay(100).Wait()
                            If IORegister(DrvID, Reg.IO_HEAD) = ODrive.AXIS_STATE_IDLE Then
                                ENCODER_READY = True
                                Exit For
                            End If
                        Next
                        Exit For
                    End If
                Next
            End If
            MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
        End If
        If MotorResult > 0 Then
            If Get_Error(DrvID, MotorID) > 0 Then MotorEvent(DrvID, MotorID, ODrive.Motor(MotorNo).ErrText)
            If Get_Error(DrvID, MotorID) < 0 Then MotorEvent(DrvID, MotorID, "ERROR: drive offline")
            If Get_Error(DrvID, MotorID) = 0 Then MotorEvent(DrvID, MotorID, "ERROR: unknown")
            Return False
        End If
        If Not ENCODER_READY Then
            MotorEvent(DrvID, MotorID, "ERROR: index fail")
            Return False
        End If

        If ENCODER_READY = True And IndexOnly = False Then
            Task.Delay(STATE_WAIT).Wait()
            MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_CLOSED_LOOP_CONTROL)
            If MotorResult > 0 Then
                If Get_Error(DrvID, MotorID) > 0 Then
                    Debug.WriteLine(ODrive.Motor(MotorNo).Text)
                End If
            End If
            CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
            Task.Delay(CMD_WAIT).Wait()
            InitPOS = Convert.ToInt32(IORegister(DrvID, Reg.IO_HEAD))
            ShadowPOS = 99999
            MotorEvent(DrvID, MotorID, "range check (A)")
            LocalHomeStartTime = System.Environment.TickCount
            For i As Integer = InitPOS To DistantLimit_P Step 100
                CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & i.ToString & ")")
                Task.Delay(CMD_WAIT).Wait()
                CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
                'Task.Delay(50).Wait()
                CurrentPOS = Convert.ToInt32(IORegister(DrvID, Reg.IO_HEAD))
                If CurrentPOS <> ShadowPOS Then
                    If Math.Abs(CurrentPOS - ShadowPOS) < HIT_Range Then
                        Task.Delay(CMD_WAIT).Wait()
                        XCnt += 1
                        If XCnt > HIT_Wait Then
                            InitPOS = CurrentPOS
                            OffsetPlus = CurrentPOS
                            Debug.Write("END_STOP(A) AT:")
                            Debug.WriteLine(CurrentPOS)
                            CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & CurrentPOS.ToString & ")")
                            Task.Delay(1000).Wait()
                            HitDetect = 1
                            Exit For
                        End If
                    Else
                        XCnt = 0
                    End If
                    ShadowPOS = CurrentPOS
                End If
                If Math.Abs(System.Environment.TickCount - LocalHomeStartTime) > LocalHomeTimeout Then
                    TimeoutDetected = True
                    Exit For
                End If
            Next

            If Not TimeoutDetected Then
                XCnt = 0
                ShadowPOS = 99999
                MotorEvent(DrvID, MotorID, "range check (B)")
                LocalHomeStartTime = System.Environment.TickCount
                For i As Integer = InitPOS To DistantLimit_M Step -100
                    CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & i.ToString & ")")
                    Task.Delay(CMD_WAIT).Wait()
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
                    'Task.Delay(50).Wait()
                    CurrentPOS = Convert.ToInt32(IORegister(DrvID, Reg.IO_HEAD))
                    If CurrentPOS <> ShadowPOS Then
                        If Math.Abs(CurrentPOS - ShadowPOS) < HIT_Range Then
                            Task.Delay(50).Wait()
                            XCnt += 1
                            If XCnt > HIT_Wait Then
                                InitPOS = CurrentPOS
                                OffsetMinus = CurrentPOS
                                Debug.Write("END_STOP(B) AT:")
                                Debug.WriteLine(CurrentPOS)
                                CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & CurrentPOS.ToString & ")")
                                Task.Delay(1000).Wait()
                                HitDetect += 1
                                Exit For
                            End If
                        Else
                            XCnt = 0
                        End If
                        ShadowPOS = CurrentPOS
                    End If
                    If Math.Abs(System.Environment.TickCount - LocalHomeStartTime) > LocalHomeTimeout Then
                        TimeoutDetected = True
                        Exit For
                    End If
                Next
            End If

            If Not TimeoutDetected Then
                CenterPOS = (OffsetMinus + OffsetPlus) / 2
                Debug.Write("OFFSET:")
                Debug.WriteLine(CenterPOS)
                If Math.Abs(OffsetMinus - OffsetPlus) < 4000 Or HitDetect < 2 Then
                    MotorEvent(DrvID, MotorID, "ERROR: range")
                    SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
                    Return False
                End If
                Task.Delay(STATE_WAIT).Wait()
                CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & CenterPOS.ToString & ")")
                For i As Integer = 0 To 100
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
                    Task.Delay(100).Wait()
                    CurrentPOS = Convert.ToInt32(IORegister(DrvID, Reg.IO_HEAD))
                    If Math.Abs(CurrentPOS - CenterPOS) < 100 Then
                        Task.Delay(STATE_WAIT).Wait()
                        Exit For
                    End If
                Next
            End If

            MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)

            If TimeoutDetected Then
                Debug.WriteLine("ERROR: Timeout")
                Return False
            End If

            If DrvID = ODrive.Map_DRV(0) And MotorID = ODrive.Map_OUT(0) Then ControlRegister(Register.Axis0_Offset) = CenterPOS
            If DrvID = ODrive.Map_DRV(1) And MotorID = ODrive.Map_OUT(1) Then ControlRegister(Register.Axis1_Offset) = CenterPOS
            If DrvID = ODrive.Map_DRV(2) And MotorID = ODrive.Map_OUT(2) Then ControlRegister(Register.Axis2_Offset) = CenterPOS
            If DrvID = ODrive.Map_DRV(3) And MotorID = ODrive.Map_OUT(3) Then ControlRegister(Register.Axis3_Offset) = CenterPOS

            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_CURRENT, ODrive.MotorPreset(PresetRUN).Calibration_Current, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIM, ODrive.MotorPreset(PresetRUN).Current_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIMIT_TOLERANCE, ODrive.MotorPreset(PresetRUN).Current_Limit_Tolerance, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.REQUESTED_CURRENT_RANGE, ODrive.MotorPreset(PresetRUN).Current_Range, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_CONTROL_BANDWIDTH, ODrive.MotorPreset(PresetRUN).Current_Control_Bandwidth, True)

            If ODrive.PositionUnit = ODrive.Unit.Pulse Then
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetRUN).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetRUN).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
            End If
            If ODrive.PositionUnit = ODrive.Unit.Turn Then
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.MotorPreset(PresetRUN).Safe_Velocity, True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(PresetRUN).Safe_Velocity, True)
            End If

            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.MotorPreset(PresetRUN).Acceleration_Limit, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.MotorPreset(PresetRUN).Deacceleration_Limit, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(PresetRUN).Position_Gain, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(PresetRUN).Velocity_Gain, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(PresetRUN).Velocity_Integrator_Gain, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, ODrive.MotorPreset(PresetRUN).Calibration_Velocity, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, ODrive.MotorPreset(PresetRUN).Calibration_Acceleration, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, ODrive.MotorPreset(PresetRUN).Calibration_Ramp, True)
            If SetResult + MotorResult = 0 Then
                Result = True
            Else
                Result = False
            End If

        End If

        If ENCODER_READY = True And IndexOnly = True Then
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_CURRENT, ODrive.MotorPreset(PresetRUN).Calibration_Current, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIM, ODrive.MotorPreset(PresetRUN).Current_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIMIT_TOLERANCE, ODrive.MotorPreset(PresetRUN).Current_Limit_Tolerance, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.REQUESTED_CURRENT_RANGE, ODrive.MotorPreset(PresetRUN).Current_Range, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_CONTROL_BANDWIDTH, ODrive.MotorPreset(PresetRUN).Current_Control_Bandwidth, True)

            If ODrive.PositionUnit = ODrive.Unit.Pulse Then
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetRUN).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetRUN).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
            End If
            If ODrive.PositionUnit = ODrive.Unit.Turn Then
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.MotorPreset(PresetRUN).Safe_Velocity, True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(PresetRUN).Safe_Velocity, True)
            End If

            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.MotorPreset(PresetRUN).Acceleration_Limit, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.MotorPreset(PresetRUN).Deacceleration_Limit, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(PresetRUN).Position_Gain, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(PresetRUN).Velocity_Gain, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(PresetRUN).Velocity_Integrator_Gain, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, ODrive.MotorPreset(PresetRUN).Calibration_Velocity, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, ODrive.MotorPreset(PresetRUN).Calibration_Acceleration, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, ODrive.MotorPreset(PresetRUN).Calibration_Ramp, True)
            Result = True
        End If

        If SetResult > 0 Then
            MotorEvent(DrvID, MotorID, "COMM ERROR")
            Result = False
        End If
        If MotorResult > 0 Then
            Dim TmpErrStr As String = ""
            If Get_Error(DrvID, MotorID) > 0 Then
                MotorEvent(DrvID, MotorID, ODrive.Motor(MotorNo).ErrText)
            Else
                MotorEvent(DrvID, MotorID, "DRV ERROR")
            End If
            Result = False
        End If

        If Result Then MotorEvent(DrvID, MotorID, "ready")


        Debug.WriteLine("finish ( T = " & (System.Environment.TickCount - TimeStamp).ToString & " ) ( Offset = " & CurrentPOS.ToString & " )")

        Return Result

    End Function

    Private Function Home_Linear(DrvID As Integer, MotorID As Integer, HomeLimit As Integer, HomeTotalance As Integer, HomeTimeout As Integer, IndexTimeout As Integer, PresetCAL As Integer, PresetRUN As Integer, IndexOnly As Boolean) As Boolean

        Dim DistantLimit_P As Integer = HomeLimit
        Dim DistantLimit_M As Integer = DistantLimit_P * -1
        Const HIT_Wait As Integer = 10
        Const HIT_Range As Integer = 30
        Dim HitDetect As Integer = 0
        Dim Result As Boolean = False
        Dim CurrentPOS As Integer = 0
        Dim ShadowPOS As Integer = 999999
        Dim InitPOS As Integer = 0
        Dim OffsetPlus As Integer = 0
        Dim OffsetMinus As Integer = 0
        Dim CenterPOS As Integer = 0
        Dim XCnt As Integer = 0
        'Dim Vel_Limit As Single = Math.Truncate((MaxRPM / 60.0) * 2048.0)
        Dim ENCODER_READY As Boolean = False
        Dim MotorResult As Integer = 0
        Dim SetResult As Integer = 0
        Dim MotorNo As Integer = -1
        Dim DriveError As Integer = 0
        Dim TimeStamp As Integer = System.Environment.TickCount
        Dim LocalHomeStartTime As Integer = 0
        Dim LocalHomeTimeout As Integer = HomeTimeout * 1000
        Dim LocalIndexTimeout As Integer = IndexTimeout * 1000
        Dim TimeoutDetected As Boolean = False
        Dim STATE_WAIT As Integer = OPConfig.Delay_StateChange
        Dim CMD_WAIT As Integer = OPConfig.Delay_ParameterSet

        Debug.WriteLine("Homing DRV:" & DrvID.ToString & " Motor:" & MotorID.ToString & " MODE:linear")
        Debug.WriteLine("Home Totalance (PPR):" & HomeTotalance.ToString)

        If DrvID >= 0 And MotorID >= 0 Then
            For i As Integer = 0 To 3
                If DrvID = ODrive.Map_DRV(i) And MotorID = ODrive.Map_OUT(i) Then
                    MotorNo = i
                    Exit For
                End If
            Next
        End If
        Select Case MotorNo
            Case 0 : ControlRegister(Register.Axis0_Offset) = 0
            Case 1 : ControlRegister(Register.Axis1_Offset) = 0
            Case 2 : ControlRegister(Register.Axis2_Offset) = 0
            Case 3 : ControlRegister(Register.Axis3_Offset) = 0
            Case 4 : ControlRegister(Register.Axis4_Offset) = 0
            Case 5 : ControlRegister(Register.Axis5_Offset) = 0
            Case 6 : ControlRegister(Register.Axis6_Offset) = 0
            Case 7 : ControlRegister(Register.Axis7_Offset) = 0
            Case Else
                Debug.WriteLine("ERROR: Invalid Mapping [" & MotorNo.ToString & "] DRV:" & DrvID.ToString & " OUT:" & MotorID.ToString)
                Return False
        End Select
        If MotorNo < 0 Then
            Debug.WriteLine("ERROR: Invalid Mapping [" & MotorNo.ToString & "] DRV:" & DrvID.ToString & " OUT:" & MotorID.ToString)
            Return False
        End If
        DriveError = Get_Error(DrvID, MotorID)
        If DriveError > 0 Then
            Debug.WriteLine("ERROR: " & Motor(MotorNo).Text)
            Return False
        End If
        If DriveError < 0 Then
            Debug.WriteLine("ERROR :Drive Offline (" & DriveError & ")")
            Return False
        End If

        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_CURRENT, 20.0, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIM, 12.0, True) ' <-- old is 14.0
        'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIMIT_TOLERANCE, 1.25, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.REQUESTED_CURRENT_RANGE, 60.0, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_CONTROL_BANDWIDTH, 1000.0, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, 10000.0, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, 10000.0, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, 25000.0, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, 25000.0, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, 95.0, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, 0.0009, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, 0.0002, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, 40.0, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, 20.0, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, 3.141593, True)

        'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_CURRENT, ODrive.MotorPreset(PresetCAL).Calibration_Current, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIM, ODrive.MotorPreset(PresetCAL).Current_Limit, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIMIT_TOLERANCE, ODrive.MotorPreset(PresetCAL).Current_Limit_Tolerance, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.REQUESTED_CURRENT_RANGE, ODrive.MotorPreset(PresetCAL).Current_Range, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_CONTROL_BANDWIDTH, ODrive.MotorPreset(PresetCAL).Current_Control_Bandwidth, True)

        If ODrive.PositionUnit = ODrive.Unit.Pulse Then
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetCAL).Velocity_Limit, ODrive.Axis(MotorNo).PPR), True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetCAL).Trajectory_Velocity_Limit, ODrive.Axis(MotorNo).PPR), True)
        End If
        If ODrive.PositionUnit = ODrive.Unit.Turn Then
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.MotorPreset(PresetCAL).Velocity_Limit, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(PresetCAL).Trajectory_Velocity_Limit, True)
        End If

        SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.MotorPreset(PresetCAL).Acceleration_Limit, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.MotorPreset(PresetCAL).Deacceleration_Limit, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(PresetCAL).Position_Gain, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(PresetCAL).Velocity_Gain, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(PresetCAL).Velocity_Integrator_Gain, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, ODrive.MotorPreset(PresetCAL).Calibration_Velocity, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, ODrive.MotorPreset(PresetCAL).Calibration_Acceleration, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, ODrive.MotorPreset(PresetCAL).Calibration_Ramp, True)
        If MotorResult > 0 Then
            If Get_Error(DrvID, MotorID) > 0 Then MotorEvent(DrvID, MotorID, ODrive.Motor(MotorNo).ErrText)
            If Get_Error(DrvID, MotorID) < 0 Then MotorEvent(DrvID, MotorID, "drive offline")
            If Get_Error(DrvID, MotorID) = 0 Then MotorEvent(DrvID, MotorID, "unknown")
            Return False
        End If

        'check if encoder is ready
        MotorEvent(DrvID, MotorID, "encoder check")
        IORegister(DrvID, Reg.IO_HEAD) = 0
        For i As Integer = 0 To 5
            CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_IS_READY))
            Task.Delay(100).Wait()
            If IORegister(DrvID, Reg.IO_HEAD) > 0 Then
                ENCODER_READY = True
                Exit For
            End If
        Next

        'index search
        If Not ENCODER_READY Then
            MotorEvent(DrvID, MotorID, "index")
            IORegister(DrvID, Reg.IO_HEAD) = 0
            MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_ENCODER_INDEX_SEARCH)
            Task.Delay(100).Wait()
            IORegister(DrvID, Reg.IO_HEAD) = 0
            For i As Integer = 0 To 100
                CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_IS_READY))
                Task.Delay(100).Wait()
                If IORegister(DrvID, Reg.IO_HEAD) > 0 Then
                    IORegister(DrvID, Reg.IO_HEAD) = 0
                    For j As Integer = 0 To 100
                        CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.CURRENT_STATE))
                        Task.Delay(100).Wait()
                        If IORegister(DrvID, Reg.IO_HEAD) = ODrive.AXIS_STATE_IDLE Then
                            ENCODER_READY = True
                            Exit For
                        End If
                    Next
                    Exit For
                End If
            Next
        End If
        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
        If MotorResult > 0 Then
            Select Case Get_Error(DrvID, MotorID)
                Case > 0 : MotorEvent(DrvID, MotorID, ODrive.Motor(MotorNo).ErrText)
                Case < 0 : MotorEvent(DrvID, MotorID, "ERROR: drive offline")
                Case = 0 : MotorEvent(DrvID, MotorID, "ERROR: unknown")
            End Select
            Return False
        End If

        'reverse index search
        If Not ENCODER_READY Then
            MotorEvent(DrvID, MotorID, "reverse index")
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, ODrive.MotorPreset(PresetCAL).Calibration_Velocity * -1.0, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, ODrive.MotorPreset(PresetCAL).Calibration_Acceleration * -1.0, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, ODrive.MotorPreset(PresetCAL).Calibration_Ramp * -1.0, True)
            If SetResult = 0 Then
                IORegister(DrvID, Reg.IO_HEAD) = 0
                MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_ENCODER_INDEX_SEARCH)
                Task.Delay(100).Wait()
                IORegister(DrvID, Reg.IO_HEAD) = 0
                For i As Integer = 1 To 100
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_IS_READY))
                    Task.Delay(100).Wait()
                    If IORegister(DrvID, Reg.IO_HEAD) > 0 Then
                        IORegister(DrvID, Reg.IO_HEAD) = 0
                        For j As Integer = 0 To 10
                            CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.CURRENT_STATE))
                            Task.Delay(100).Wait()
                            If IORegister(DrvID, Reg.IO_HEAD) = ODrive.AXIS_STATE_IDLE Then
                                ENCODER_READY = True
                                Exit For
                            End If
                        Next
                        Exit For
                    End If
                Next
            End If
            MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
        End If
        If MotorResult > 0 Then
            If Get_Error(DrvID, MotorID) > 0 Then MotorEvent(DrvID, MotorID, ODrive.Motor(MotorNo).ErrText)
            If Get_Error(DrvID, MotorID) < 0 Then MotorEvent(DrvID, MotorID, "ERROR: drive offline")
            If Get_Error(DrvID, MotorID) = 0 Then MotorEvent(DrvID, MotorID, "ERROR: unknown")
            Return False
        End If
        If Not ENCODER_READY Then
            MotorEvent(DrvID, MotorID, "ERROR: index fail")
            Return False
        End If

        If ENCODER_READY = True And IndexOnly = False Then
            Task.Delay(STATE_WAIT).Wait()
            MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_CLOSED_LOOP_CONTROL)
            If MotorResult > 0 Then
                If Get_Error(DrvID, MotorID) > 0 Then
                    Debug.WriteLine(ODrive.Motor(MotorNo).Text)
                End If
            End If
            CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
            Task.Delay(CMD_WAIT).Wait()
            InitPOS = Convert.ToInt32(IORegister(DrvID, Reg.IO_HEAD))
            ShadowPOS = 99999
            MotorEvent(DrvID, MotorID, "range check (A)")
            LocalHomeStartTime = System.Environment.TickCount
            For i As Integer = InitPOS To DistantLimit_P Step 100
                CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & i.ToString & ")")
                Task.Delay(CMD_WAIT).Wait()
                CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
                'Task.Delay(50).Wait()
                CurrentPOS = Convert.ToInt32(IORegister(DrvID, Reg.IO_HEAD))
                If CurrentPOS <> ShadowPOS Then
                    If Math.Abs(CurrentPOS - ShadowPOS) < HIT_Range Then
                        Task.Delay(CMD_WAIT).Wait()
                        XCnt += 1
                        If XCnt > HIT_Wait Then
                            InitPOS = CurrentPOS
                            OffsetPlus = CurrentPOS
                            Debug.Write("END_STOP(A) AT:")
                            Debug.WriteLine(CurrentPOS)
                            CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & CurrentPOS.ToString & ")")
                            Task.Delay(1000).Wait()
                            HitDetect = 1
                            Exit For
                        End If
                    Else
                        XCnt = 0
                    End If
                    ShadowPOS = CurrentPOS
                End If
                If Math.Abs(System.Environment.TickCount - LocalHomeStartTime) > LocalHomeTimeout Then
                    TimeoutDetected = True
                    Exit For
                End If
            Next

            If Not TimeoutDetected Then
                XCnt = 0
                ShadowPOS = 99999
                MotorEvent(DrvID, MotorID, "range check (B)")
                LocalHomeStartTime = System.Environment.TickCount
                For i As Integer = InitPOS To DistantLimit_M Step -100
                    CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & i.ToString & ")")
                    Task.Delay(CMD_WAIT).Wait()
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
                    'Task.Delay(50).Wait()
                    CurrentPOS = Convert.ToInt32(IORegister(DrvID, Reg.IO_HEAD))
                    If CurrentPOS <> ShadowPOS Then
                        If Math.Abs(CurrentPOS - ShadowPOS) < HIT_Range Then
                            Task.Delay(50).Wait()
                            XCnt += 1
                            If XCnt > HIT_Wait Then
                                InitPOS = CurrentPOS
                                OffsetMinus = CurrentPOS
                                Debug.Write("END_STOP(B) AT:")
                                Debug.WriteLine(CurrentPOS)
                                CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & CurrentPOS.ToString & ")")
                                Task.Delay(1000).Wait()
                                HitDetect += 1
                                Exit For
                            End If
                        Else
                            XCnt = 0
                        End If
                        ShadowPOS = CurrentPOS
                    End If
                    If Math.Abs(System.Environment.TickCount - LocalHomeStartTime) > LocalHomeTimeout Then
                        TimeoutDetected = True
                        Exit For
                    End If
                Next
            End If

            If Not TimeoutDetected Then
                CenterPOS = (OffsetMinus + OffsetPlus) / 2
                Debug.Write("OFFSET:")
                Debug.WriteLine(CenterPOS)
                If Math.Abs(OffsetMinus - OffsetPlus) < 4000 Or HitDetect < 2 Then
                    MotorEvent(DrvID, MotorID, "ERROR: range")
                    SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
                    Return False
                End If
                Task.Delay(STATE_WAIT).Wait()
                CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & CenterPOS.ToString & ")")
                For i As Integer = 0 To 100
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
                    Task.Delay(100).Wait()
                    CurrentPOS = Convert.ToInt32(IORegister(DrvID, Reg.IO_HEAD))
                    If Math.Abs(CurrentPOS - CenterPOS) < 100 Then
                        Task.Delay(STATE_WAIT).Wait()
                        Exit For
                    End If
                Next
            End If

            MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)

            If TimeoutDetected Then
                Debug.WriteLine("ERROR: Timeout")
                Return False
            End If

            If DrvID = ODrive.Map_DRV(0) And MotorID = ODrive.Map_OUT(0) Then ControlRegister(Register.Axis0_Offset) = CenterPOS
            If DrvID = ODrive.Map_DRV(1) And MotorID = ODrive.Map_OUT(1) Then ControlRegister(Register.Axis1_Offset) = CenterPOS
            If DrvID = ODrive.Map_DRV(2) And MotorID = ODrive.Map_OUT(2) Then ControlRegister(Register.Axis2_Offset) = CenterPOS
            If DrvID = ODrive.Map_DRV(3) And MotorID = ODrive.Map_OUT(3) Then ControlRegister(Register.Axis3_Offset) = CenterPOS


            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_CURRENT, 10.0, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIM, ODrive.MotorPreset(PresetID).Current_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIMIT_TOLERANCE, ODrive.MotorPreset(PresetID).Current_Limit_Tolerance, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.REQUESTED_CURRENT_RANGE, ODrive.MotorPreset(PresetID).Current_Range, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_CONTROL_BANDWIDTH, 500.0, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.SlowPreset(PresetID).Velocity_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.SlowPreset(PresetID).Velocity_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.SlowPreset(PresetID).Acceleration_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.SlowPreset(PresetID).Deacceleration_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(PresetID).Position_Gain, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(PresetID).Velocity_Gain, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(PresetID).Velocity_Integrator_Gain, True)

            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_CURRENT, ODrive.MotorPreset(PresetRUN).Calibration_Current, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIM, ODrive.MotorPreset(PresetRUN).Current_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIMIT_TOLERANCE, ODrive.MotorPreset(PresetRUN).Current_Limit_Tolerance, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.REQUESTED_CURRENT_RANGE, ODrive.MotorPreset(PresetRUN).Current_Range, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_CONTROL_BANDWIDTH, ODrive.MotorPreset(PresetRUN).Current_Control_Bandwidth, True)

            If ODrive.PositionUnit = ODrive.Unit.Pulse Then
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetRUN).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetRUN).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
            End If
            If ODrive.PositionUnit = ODrive.Unit.Turn Then
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.MotorPreset(PresetRUN).Safe_Velocity, True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(PresetRUN).Safe_Velocity, True)
            End If

            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.MotorPreset(PresetRUN).Acceleration_Limit, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.MotorPreset(PresetRUN).Deacceleration_Limit, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(PresetRUN).Position_Gain, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(PresetRUN).Velocity_Gain, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(PresetRUN).Velocity_Integrator_Gain, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, ODrive.MotorPreset(PresetRUN).Calibration_Velocity, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, ODrive.MotorPreset(PresetRUN).Calibration_Acceleration, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, ODrive.MotorPreset(PresetRUN).Calibration_Ramp, True)
            If SetResult + MotorResult = 0 Then
                Result = True
            Else
                Result = False
            End If

        End If

        If ENCODER_READY = True And IndexOnly = True Then
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_CURRENT, 10.0, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIM, ODrive.MotorPreset(PresetID).Current_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIMIT_TOLERANCE, ODrive.MotorPreset(PresetID).Current_Limit_Tolerance, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.REQUESTED_CURRENT_RANGE, ODrive.MotorPreset(PresetID).Current_Range, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_CONTROL_BANDWIDTH, 500.0, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.SlowPreset(PresetID).Velocity_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.SlowPreset(PresetID).Velocity_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.SlowPreset(PresetID).Acceleration_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.SlowPreset(PresetID).Deacceleration_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(PresetID).Position_Gain, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(PresetID).Velocity_Gain, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(PresetID).Velocity_Integrator_Gain, True)

            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_CURRENT, ODrive.MotorPreset(PresetRUN).Calibration_Current, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIM, ODrive.MotorPreset(PresetRUN).Current_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIMIT_TOLERANCE, ODrive.MotorPreset(PresetRUN).Current_Limit_Tolerance, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.REQUESTED_CURRENT_RANGE, ODrive.MotorPreset(PresetRUN).Current_Range, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_CONTROL_BANDWIDTH, ODrive.MotorPreset(PresetRUN).Current_Control_Bandwidth, True)

            If ODrive.PositionUnit = ODrive.Unit.Pulse Then
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetRUN).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetRUN).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
            End If
            If ODrive.PositionUnit = ODrive.Unit.Turn Then
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.MotorPreset(PresetRUN).Safe_Velocity, True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(PresetRUN).Safe_Velocity, True)
            End If

            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.MotorPreset(PresetRUN).Acceleration_Limit, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.MotorPreset(PresetRUN).Deacceleration_Limit, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(PresetRUN).Position_Gain, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(PresetRUN).Velocity_Gain, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(PresetRUN).Velocity_Integrator_Gain, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, ODrive.MotorPreset(PresetRUN).Calibration_Velocity, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, ODrive.MotorPreset(PresetRUN).Calibration_Acceleration, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, ODrive.MotorPreset(PresetRUN).Calibration_Ramp, True)
            Result = True
        End If

        If SetResult > 0 Then
            MotorEvent(DrvID, MotorID, "COMM ERROR")
            Result = False
        End If
        If MotorResult > 0 Then
            Dim TmpErrStr As String = ""
            If Get_Error(DrvID, MotorID) > 0 Then
                MotorEvent(DrvID, MotorID, ODrive.Motor(MotorNo).ErrText)
            Else
                MotorEvent(DrvID, MotorID, "DRV ERROR")
            End If
            Result = False
        End If

        If Result Then MotorEvent(DrvID, MotorID, "ready")


        Debug.WriteLine("finish ( T = " & (System.Environment.TickCount - TimeStamp).ToString & " )")

        Return Result

    End Function

    Private Function Home_Gear_Multi() As Integer

        Const Controller_Input_Count As Integer = 4

        Dim res As Integer = 0

        Dim Result(10) As Boolean
        'Dim Axis_OK(10) As Boolean
        Dim MOVE_OK As Boolean = False
        Dim Sensor_OK As Boolean = True
        Dim Sensor_Value(10) As Integer
        Dim Sensor_Diff As Single = 0.0
        Dim Move_Request As Integer = 0
        Dim CurrentPOS As Single = 0.0
        Dim EncDir As Integer = 1
        Dim SetResult(10) As Integer
        Dim MotorResult(10) As Integer
        Dim DriveError(10) As Integer
        Dim MotorNo(10) As Integer
        Dim SensorNo(10) As Integer
        Dim SensorMode As Integer = -1
        Dim TimeStamp As Integer = System.Environment.TickCount
        Dim ReverseFlag As Single = 1.0
        Dim ENCODER_READY(10) As Boolean
        Dim STATE_WAIT As Integer = OPConfig.Delay_StateChange
        Dim CMD_WAIT As Integer = OPConfig.Delay_ParameterSet
        Dim DrvID As Integer = -1
        Dim MotorID As Integer = -1
        Dim IndexREQ(10) As Boolean
        Dim TimeCNT(10) As Integer
        Dim AxTOTAL As Integer = 0
        Dim AxSTATE As Integer = 0
        Dim MxState(10) As Boolean
        Dim MxIndex(10) As Boolean
        Dim MxRequest(10) As Boolean
        Dim MxPos(10) As Single
        Dim MxOffset(10) As Single

        Debug.WriteLine("+++ HOME MULTI X +++")

        'MxState(0) = True
        'MxState(1) = True
        'MxState(2) = True
        'MxState(3) = True

        For i As Integer = 0 To 9
            'Axis_OK(i) = False
            ENCODER_READY(i) = False
            DriveError(i) = 0
            IndexREQ(i) = False
            MotorNo(i) = -1
            MotorResult(i) = 0
            Result(i) = False
            SensorNo(i) = -1
            Sensor_Value(i) = -1
            SetResult(i) = 0
            TimeCNT(i) = 0
        Next

        For i As Integer = 0 To 7
            If ODrive.Map_DRV(i) >= 0 And ODrive.Map_OUT(i) >= 0 And ODrive.Map_SW(i) >= 0 Then
                Debug.WriteLine("Ax" & i.ToString & " : Drive " & ODrive.Map_DRV(i).ToString & " Motor " & ODrive.Map_OUT(i).ToString & " Sensor " & ODrive.Map_SW(i).ToString)
                If ODrive.Axis(i).EndstopEnable Then
                    'Axis_OK(i) = True
                    MxState(i) = True
                End If
            Else
                Debug.WriteLine("Ax" & i.ToString & " Home Disable")
            End If
        Next

        ' --- get sensor state
        For i As Integer = 1 To 3
            CommandProcessor("[0]DRIVE(4).WRITE(H)")
            Task.Delay(250).Wait()
        Next
        For i As Integer = 0 To 7
            If ODrive.Map_SW(i) >= 0 And ODrive.Map_SW(i) < Controller_Input_Count Then
                Sensor_Value(i) = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), ODrive.Map_SW(i))
            End If
        Next

        ' --- check axis error
        For i As Integer = 0 To 7
            ODrive.Motor(i).Ready = False
            If ODrive.Map_DRV(i) >= 0 And ODrive.Map_OUT(i) >= 0 Then
                DriveError(i) = Get_Error(ODrive.Map_DRV(i), ODrive.Map_OUT(i))
                If DriveError(i) > 0 Then Debug.WriteLine("ERROR: " & Motor(i).Text & " (" & i.ToString & ")")
                If DriveError(i) < 0 Then Debug.WriteLine("ERROR: Drive Offline (" & DriveError(i).ToString & ")")
            End If
        Next

        MxState = Home_CalibrationParameter_Update_Multi(MxState)
        MxState = Home_IndexSearch_Multi(False, MxState)

        ' --- Move Home : End stop sensor
        If True Then
            'Debug.WriteLine("ESO:" & AxisParam.EndstopOffset.ToString & " " & SensorNo.ToString)
            'MotorEvent(DrvID, MotorID, "get index position")
            MxPos = Home_getEncoderPosition_Multi(MxState)



            ' if encoder position is not center -> force index search
            For i As Integer = 0 To 7
                If MxPos(i) > 0.15 Then MxRequest(i) = True Else MxRequest(i) = False
                If MxRequest(i) Then Debug.WriteLine("Axis " & i.ToString & " index request")
            Next
            MxIndex = Home_IndexSearch_Multi(True, MxRequest)
            For i As Integer = 0 To 7
                If MxRequest(i) = True And MxIndex(i) = False Then MxState(i) = False
            Next



            For i As Integer = 0 To 7
                MxOffset(i) = ODrive.Axis(i).EndstopOffset ' load home offset
                MxPos(i) = 0.0

            Next

            Debug.WriteLine("That is no moon")
            Return False

            For i As Integer = 0 To 110 * 1000

                For ii As Integer = 0 To 7
                    If MxState(i) Then

                    End If
                Next

            Next

            '    If ENCODER_READY Then
            '        Task.Delay(STATE_WAIT).Wait()
            '        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_CLOSED_LOOP_CONTROL)
            '        Task.Delay(STATE_WAIT).Wait()
            '        Dim hPOS As Single = 0.0
            '        Dim hOffset As Single = AxisParam.EndstopOffset ' 12.0
            '        Dim xtimeout As Integer = 10 * 1000 'sec
            '        ' --- move to home sensor
            '        For ii As Integer = 0 To xtimeout
            '            Task.Delay(25).Wait()
            '            hPOS -= 0.025
            '            CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & hPOS.ToString & ")")
            '            Task.Delay(25).Wait()
            '            CommandProcessor("[0]DRIVE(4).WRITE(H)")
            '            Select Case SensorNo
            '                Case 0 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 0)
            '                Case 1 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 1)
            '                Case 2 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 2)
            '                Case 3 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 3)
            '            End Select
            '            If Sensor_Value > 0 Then Exit For
            '        Next
            '        Task.Delay(1000).Wait()
            '        ' --- move out of home sensor
            '        For ii As Integer = 0 To 300
            '            Task.Delay(50).Wait()
            '            hPOS += 0.01
            '            CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & hPOS.ToString & ")")
            '            Task.Delay(50).Wait()
            '            CommandProcessor("[0]DRIVE(4).WRITE(H)")
            '            Select Case SensorNo
            '                Case 0 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 0)
            '                Case 1 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 1)
            '                Case 2 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 2)
            '                Case 3 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 3)
            '            End Select
            '            If Sensor_Value = 0 Then Exit For
            '        Next
            '        ' --- move to home position
            '        Task.Delay(100).Wait()
            '        CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & (hPOS + hOffset).ToString & ")")
            '        For j As Integer = 0 To 300
            '            CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
            '            Task.Delay(100).Wait()
            '            If Math.Abs(IORegister(DrvID, Reg.IO_HEAD) - (hPOS + hOffset)) < 0.015 Then Exit For
            '        Next
            '        Task.Delay(1000).Wait()
            '        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
            '        ' --- init encoder index
            '        Task.Delay(3000).Wait()
            '        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_ENCODER_INDEX_SEARCH)
            '        Task.Delay(250).Wait()
            '        For j As Integer = 0 To 100
            '            CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.CURRENT_STATE))
            '            Task.Delay(100).Wait()
            '            If IORegister(DrvID, Reg.IO_HEAD) = ODrive.AXIS_STATE_IDLE Then Exit For
            '        Next
            '        Debug.WriteLine("END")
            '    End If
            '    Return False
            'End If

        End If

        '' --- Move Home : End stop sensor
        ''AxisParam.IndexEnable And AxisParam.HomeEnable = False And AxisParam.EndstopEnable
        'If AxSTATE > 0 Then
        '    'Debug.WriteLine("ESO:" & AxisParam.EndstopOffset.ToString & " " & SensorNo.ToString)
        '    'MotorEvent(DrvID, MotorID, "get index position")

        '    For i As Integer = 1 To 3
        '        CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
        '        Task.Delay(CMD_WAIT).Wait()
        '    Next

        '    ' if encoder position is not center -> force index search
        '    If Math.Abs(IORegister(DrvID, Reg.IO_HEAD)) > 0.15 Then
        '        Debug.WriteLine("force index search")
        '        ENCODER_READY = False
        '        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
        '        Task.Delay(250).Wait()
        '        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_ENCODER_INDEX_SEARCH)
        '        Task.Delay(250).Wait()
        '        For j As Integer = 0 To 100
        '            CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.CURRENT_STATE))
        '            Task.Delay(100).Wait()
        '            If IORegister(DrvID, Reg.IO_HEAD) = ODrive.AXIS_STATE_IDLE Then
        '                ENCODER_READY = True
        '                Exit For
        '            End If
        '        Next
        '    End If

        '    'Debug.WriteLine("That is no moon")
        '    'Return False
        '    If ENCODER_READY Then
        '        Task.Delay(STATE_WAIT).Wait()
        '        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_CLOSED_LOOP_CONTROL)
        '        Task.Delay(STATE_WAIT).Wait()
        '        Dim hPOS As Single = 0.0
        '        Dim hOffset As Single = AxisParam.EndstopOffset ' 12.0
        '        Dim xtimeout As Integer = 10 * 1000 'sec
        '        ' --- move to home sensor
        '        For ii As Integer = 0 To xtimeout
        '            Task.Delay(25).Wait()
        '            hPOS -= 0.025
        '            CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & hPOS.ToString & ")")
        '            Task.Delay(25).Wait()
        '            CommandProcessor("[0]DRIVE(4).WRITE(H)")
        '            Select Case SensorNo
        '                Case 0 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 0)
        '                Case 1 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 1)
        '                Case 2 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 2)
        '                Case 3 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 3)
        '            End Select
        '            If Sensor_Value > 0 Then Exit For
        '        Next
        '        Task.Delay(1000).Wait()
        '        ' --- move out of home sensor
        '        For ii As Integer = 0 To 300
        '            Task.Delay(50).Wait()
        '            hPOS += 0.01
        '            CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & hPOS.ToString & ")")
        '            Task.Delay(50).Wait()
        '            CommandProcessor("[0]DRIVE(4).WRITE(H)")
        '            Select Case SensorNo
        '                Case 0 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 0)
        '                Case 1 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 1)
        '                Case 2 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 2)
        '                Case 3 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 3)
        '            End Select
        '            If Sensor_Value = 0 Then Exit For
        '        Next
        '        ' --- move to home position
        '        Task.Delay(100).Wait()
        '        CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & (hPOS + hOffset).ToString & ")")
        '        For j As Integer = 0 To 300
        '            CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
        '            Task.Delay(100).Wait()
        '            If Math.Abs(IORegister(DrvID, Reg.IO_HEAD) - (hPOS + hOffset)) < 0.015 Then Exit For
        '        Next
        '        Task.Delay(1000).Wait()
        '        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
        '        ' --- init encoder index
        '        Task.Delay(3000).Wait()
        '        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_ENCODER_INDEX_SEARCH)
        '        Task.Delay(250).Wait()
        '        For j As Integer = 0 To 100
        '            CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.CURRENT_STATE))
        '            Task.Delay(100).Wait()
        '            If IORegister(DrvID, Reg.IO_HEAD) = ODrive.AXIS_STATE_IDLE Then Exit For
        '        Next
        '        Debug.WriteLine("END")
        '    End If
        '    Return False
        'End If
        Debug.WriteLine("it's a trap !")

        For Each iX As Boolean In MxState
            Debug.WriteLine(iX)
        Next

        Return res
    End Function

    Private Function Home_getEncoderPosition_Multi(xAxisEnable() As Boolean) As Single()
        Dim res(10) As Single
        Dim DrvID As Integer
        Dim MotorID As Integer
        Dim STATE_READY(10) As Boolean

        System.Array.Clear(STATE_READY, 0, STATE_READY.Length)
        For i As Integer = 0 To xAxisEnable.Length - 1
            STATE_READY(i) = xAxisEnable(i)
        Next

        For i As Integer = 0 To 7
            DrvID = ODrive.Map_DRV(i)
            MotorID = ODrive.Map_OUT(i)
            If DrvID >= 0 And MotorID >= 0 And STATE_READY(i) Then
                For j As Integer = 1 To 3
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
                    Task.Delay(100).Wait()
                Next
                res(i) = Math.Abs(IORegister(DrvID, Reg.IO_HEAD))
            End If
        Next


        ' if encoder position is not center -> force index search
        Return res
    End Function

    Private Function Home_CalibrationParameter_Update_Multi(xAxisEnable() As Boolean) As Boolean()
        Dim DrvID As Integer
        Dim MotorID As Integer
        Dim MotorResult(10) As Integer
        Dim SetResult(10) As Integer
        Dim STATE_READY(10) As Boolean

        System.Array.Clear(STATE_READY, 0, STATE_READY.Length)
        For i As Integer = 0 To xAxisEnable.Length - 1
            STATE_READY(i) = xAxisEnable(i)
        Next

        ' --- odrive calibration parameter init
        For i As Integer = 0 To 7
            DrvID = ODrive.Map_DRV(i)
            MotorID = ODrive.Map_OUT(i)
            If DrvID >= 0 And MotorID >= 0 And STATE_READY(i) Then
                Debug.WriteLine("Calibration parameter update (Axis:" & i.ToString & " Profile ID:" & ODrive.Axis(i).CalibrationProfile.ToString & ")")
                MotorResult(i) += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
                If ODrive.PositionUnit = ODrive.Unit.Pulse Then
                    SetResult(i) += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Velocity_Limit, ODrive.Axis(i).PPR), True)
                    SetResult(i) += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Trajectory_Velocity_Limit, ODrive.Axis(i).PPR), True)
                End If
                If ODrive.PositionUnit = ODrive.Unit.Turn Then
                    SetResult(i) += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Velocity_Limit, True)
                    SetResult(i) += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Trajectory_Velocity_Limit, True)
                End If
                SetResult(i) += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Acceleration_Limit, True)
                SetResult(i) += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Deacceleration_Limit, True)
                SetResult(i) += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Position_Gain, True)
                SetResult(i) += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Velocity_Gain, True)
                SetResult(i) += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Velocity_Integrator_Gain, True)
                SetResult(i) += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Calibration_Velocity, True)
                SetResult(i) += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Calibration_Acceleration, True)
                SetResult(i) += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Calibration_Ramp, True)
            End If
        Next

        For i As Integer = 0 To 7
            If STATE_READY(i) Then
                If ODrive.Axis(i).EndstopEnable = False And ODrive.Axis(i).HomeEnable = False Then Debug.WriteLine(" --- ! WARNING ! --- Axis:" & i.ToString & " use IndexOnly MODE (Encoder offset value is ignore and can cause PHYSICAL DAMAGE)")
            End If
        Next

        ' --- parameter init error trap
        For i As Integer = 0 To 7
            If SetResult(i) > 0 Then
                MotorEvent(DrvID, MotorID, "ERROR: COMM")
                STATE_READY(i) = False
            End If
            If MotorResult(i) > 0 Then
                Select Case Get_Error(DrvID, MotorID)
                    Case > 0 : MotorEvent(DrvID, MotorID, "ERROR: " & ODrive.Motor(i).ErrText)
                    Case < 0 : MotorEvent(DrvID, MotorID, "ERROR: drive offline")
                    Case = 0 : MotorEvent(DrvID, MotorID, "ERROR: unknown")
                End Select
                STATE_READY(i) = False
            End If
        Next

        Return STATE_READY
    End Function

    Private Function Home_IndexSearch_Multi(forceSearch As Boolean, xAxisEnable() As Boolean) As Boolean()
        Dim DrvID As Integer
        Dim MotorID As Integer
        Dim AxTOTAL As Integer = 0
        Dim AxSTATE As Integer = 0
        Dim MotorResult(10) As Integer
        Dim STATE_READY(10) As Boolean
        Dim AXIS_ENABLE(10) As Boolean

        System.Array.Clear(MotorResult, 0, MotorResult.Length)
        System.Array.Clear(STATE_READY, 0, STATE_READY.Length)
        System.Array.Clear(AXIS_ENABLE, 0, AXIS_ENABLE.Length)
        For i As Integer = 0 To xAxisEnable.Length - 1
            AXIS_ENABLE(i) = xAxisEnable(i)
        Next

        ' ---- check if encoder is ready
        If forceSearch = False Then
            For i As Integer = 0 To 7
                DrvID = ODrive.Map_DRV(i)
                MotorID = ODrive.Map_OUT(i)
                If DrvID >= 0 And MotorID >= 0 Then
                    MotorEvent(DrvID, MotorID, "axis:" & i.ToString & " motor encoder check")
                    IORegister(DrvID, Reg.IO_HEAD) = 0
                    For j As Integer = 0 To 3
                        CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_IS_READY))
                        Task.Delay(100).Wait()
                        If IORegister(DrvID, Reg.IO_HEAD) > 0 Then
                            STATE_READY(i) = True
                            Exit For
                        End If
                    Next
                End If
            Next
            IORegister(0, Reg.IO_HEAD) = 0
            IORegister(1, Reg.IO_HEAD) = 0
            IORegister(2, Reg.IO_HEAD) = 0
            IORegister(3, Reg.IO_HEAD) = 0
        End If
        ' ---- if encoder is not ready -> do index search
        For i As Integer = 0 To 7
            DrvID = ODrive.Map_DRV(i)
            MotorID = ODrive.Map_OUT(i)
            If DrvID >= 0 And MotorID >= 0 And AXIS_ENABLE(i) Then
                AxTOTAL += 1
                If (Not STATE_READY(i)) And ODrive.Axis(i).IndexEnable Then
                    MotorEvent(DrvID, MotorID, "Axis: " & i.ToString & "index")
                    MotorResult(i) += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_ENCODER_INDEX_SEARCH)
                End If
            End If
        Next
        Task.Delay(100).Wait()
        ' ---- wait until index complete Or timeout
        For i As Integer = 0 To 10
            AxSTATE = 0
            For j As Integer = 0 To 7
                DrvID = ODrive.Map_DRV(j)
                MotorID = ODrive.Map_OUT(j)
                If DrvID >= 0 And MotorID >= 0 And AXIS_ENABLE(i) Then
                    If Not STATE_READY(j) Then
                        CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_IS_READY))
                        Task.Delay(100).Wait()
                        If IORegister(DrvID, Reg.IO_HEAD) > 0 Then
                            IORegister(DrvID, Reg.IO_HEAD) = 0
                            STATE_READY(j) = True
                            Exit For
                        End If
                    Else
                        AxSTATE += 1
                    End If
                End If
            Next
            If AxSTATE = AxTOTAL Then ' --- check if all index complete
                Exit For
            End If
        Next

        If AxSTATE <> AxTOTAL Then ' --- index timeout trap
            For i As Integer = 0 To 7
                If ODrive.Map_DRV(i) >= 0 And ODrive.Map_OUT(i) >= 0 And STATE_READY(i) = False And AXIS_ENABLE(i) Then
                    Debug.WriteLine("ERROR: Axis " & i.ToString & " Timeout")
                End If
            Next
        End If

        For i As Integer = 0 To 10
            AxSTATE = 0
            For j As Integer = 0 To 7 ' --- refresh motor state
                DrvID = ODrive.Map_DRV(j)
                MotorID = ODrive.Map_OUT(j)
                If AXIS_ENABLE(i) Then
                    If DrvID >= 0 And MotorID >= 0 And AXIS_ENABLE(i) Then
                        CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.CURRENT_STATE))
                        Task.Delay(100).Wait()
                        If IORegister(DrvID, Reg.IO_HEAD) = ODrive.AXIS_STATE_IDLE Then
                            AxSTATE += 1
                            STATE_READY(i) = True
                        End If
                    End If
                Else
                    Task.Delay(100).Wait()
                End If

            Next
            If AxSTATE = AxTOTAL Then ' --- if all motor in idle state then end
                Exit For
            End If
        Next

        AxSTATE = 0
        For i As Integer = 0 To 7
            DrvID = ODrive.Map_DRV(i)
            MotorID = ODrive.Map_OUT(i)
            If DrvID >= 0 And MotorID >= 0 Then
                IORegister(DrvID, Reg.IO_HEAD) = 0
                For j As Integer = 0 To 3
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.CURRENT_STATE))
                    Task.Delay(100).Wait()
                    If IORegister(DrvID, Reg.IO_HEAD) = ODrive.AXIS_STATE_IDLE Then
                        STATE_READY(i) = True
                        Exit For
                    End If
                Next
            End If
        Next

        If AxSTATE <> AxTOTAL Then ' --- index motor error trap
            Debug.WriteLine("ERROR: motor not idle state")
        End If

        Return STATE_READY
    End Function

    'Private Function Home_Gear(DrvID As Integer, MotorID As Integer, HomeDegree As Single, HomeLimit As Single, HomeTotalance As Single, HomeTimeout As Integer, IndexTimeout As Integer, PPR As Integer, GearRatio As Single, Reverse As Boolean, PresetCAL As Integer, PresetRUN As Integer, IndexOnly As Boolean, IndexEnable As Boolean) As Boolean
    Private Function Home_Gear(DrvID As Integer, MotorID As Integer, AxisParam As ODrive.AxisParameter) As Boolean

        'Const CMD_Wait As Integer = 50
        'Const STATE_Wait As Integer = 250
        ' Const HomeTotalance As Single = 200.0 ' ppr unit
        ' Const HomeTimeout As Integer = 10000 ' msec unit (minimum 100)
        ' Const IndexTimeout As Integer = 10000 ' msec unit (minimum 100)

        Dim Result As Boolean = False
        Dim MOVE_OK As Boolean = False
        Dim Sensor_OK As Boolean = True
        'Dim Comm_OK As Boolean = True
        Dim Sensor_Value As Single = 0.0
        Dim Sensor_Diff As Single = 0.0
        Dim Move_Request As Integer = 0
        Dim CurrentPOS As Single = 0.0
        Dim EncDir As Integer = 1
        Dim SetResult As Integer = 0
        Dim MotorResult As Integer = 0
        Dim DriveError As Integer = 0
        Dim MotorNo As Integer = -1
        Dim SensorNo As Integer = -1
        Dim SensorMode As Integer = -1
        Dim SensorType As Integer = 1
        Dim TimeStamp As Integer = System.Environment.TickCount
        Dim ReverseFlag As Single = 1.0
        Dim ENCODER_READY As Boolean = False
        'Dim DisplayPageID As Integer = 1
        Dim STATE_WAIT As Integer = OPConfig.Delay_StateChange
        Dim CMD_WAIT As Integer = OPConfig.Delay_ParameterSet

        Dim LocalHomeTotalance As Integer = Math.Truncate(((AxisParam.PPR * AxisParam.GearRatio) / 360.0) * AxisParam.HomeTotalance)
        Dim LocalHomeTimeout As Integer = AxisParam.HomeTimeout
        Dim LocalIndexTimeout As Integer = AxisParam.IndexTimeout

        Dim StartTick As Integer = 0
        Dim EndTick As Integer = 0

        SysEvent("Homing DRV:" & DrvID.ToString & " OUT:" & MotorID.ToString & " MODE:circular")
        'Debug.WriteLine("Home Totalance (PPR):" & LocalHomeTotalance.ToString)
        If AxisParam.HomeDir Then EncDir = -1

        If DrvID >= 0 And MotorID >= 0 Then
            For i As Integer = 0 To 3
                If DrvID = ODrive.Map_DRV(i) And MotorID = ODrive.Map_OUT(i) Then
                    MotorNo = i
                    SensorNo = ODrive.Map_SW(i)
                    Exit For
                End If
            Next
        End If
        Select Case SensorType
            Case = 0 ' Absolute Encoder
                For i As Integer = 1 To 6
                    CommandProcessor("[0]DRIVE(4).WRITE(E)")
                    Task.Delay(250).Wait()
                Next
                Select Case MotorNo
                    Case 0 : ControlRegister(Register.Axis0_Offset) = 0 : Sensor_Value = IORegister(Reg.Device_SystemIO, Reg.IO_X)
                    Case 1 : ControlRegister(Register.Axis1_Offset) = 0 : Sensor_Value = IORegister(Reg.Device_SystemIO, Reg.IO_Y)
                    Case 2 : ControlRegister(Register.Axis2_Offset) = 0 : Sensor_Value = IORegister(Reg.Device_SystemIO, Reg.IO_Z)
                    Case 3 : ControlRegister(Register.Axis3_Offset) = 0 : Sensor_Value = IORegister(Reg.Device_SystemIO, Reg.IO_A)
                    Case Else
                        Debug.WriteLine("ERROR: Axis [" & MotorNo.ToString & "] not support circular homing")
                        Return False
                End Select
            Case = 1 ' Home Switch
                If AxisParam.EndstopEnable Then
                    For i As Integer = 1 To 3
                        CommandProcessor("[0]DRIVE(4).WRITE(H)")
                        Task.Delay(250).Wait()
                    Next
                    Select Case SensorNo
                        Case 0 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 0)
                        Case 1 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 1)
                        Case 2 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 2)
                        Case 3 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 3)
                        Case Else
                            Debug.WriteLine("ERROR: Axis [" & MotorNo.ToString & "] not support circular homing")
                            Return False
                    End Select
                End If
        End Select

        If MotorNo < 0 Then
            Debug.WriteLine("ERROR: Invalid motor mapping [" & MotorNo.ToString & "] DRV:" & DrvID.ToString & " OUT:" & MotorID.ToString)
            Return False
        End If
        If SensorType <= 0 And SensorNo < 0 And AxisParam.HomeEnable Then
            Debug.WriteLine("ERROR: Invalid sensor mapping [" & MotorNo.ToString & "] SW:" & SensorNo.ToString)
            Return False
        End If

        If (Not AxisParam.EndstopEnable) And AxisParam.HomeEnable Then
            If Sensor_Value > 3000.0 Then Sensor_Value = Sensor_Value - 3000.0 '---- IGNORE MAGNETIC FIELD ERROR ----
            If Sensor_Value > 2000.0 Then Sensor_Value = Sensor_Value - 2000.0 '---- IGNORE MAGNETIC FIELD ERROR ----
            If Sensor_Value > 1000.0 Then Sensor_Value = Sensor_Value - 1000.0 '---- IGNORE MAGNETIC FIELD ERROR ----
            If Sensor_Value > 360.0 Then Sensor_OK = False
            If Sensor_Value < 0.1 Then Sensor_OK = False
            If Sensor_OK Then
                If Math.Abs(AxisMath.GetCircleShortPath(Sensor_Value, AxisParam.HomeDeg)) > AxisParam.HomeLimit Then
                    Debug.Write("ERROR:home distant out of range " & AxisMath.GetCircleShortPath(Sensor_Value, AxisParam.HomeDeg).ToString)
                    Debug.WriteLine(" " & Sensor_Value.ToString & " -> " & AxisParam.HomeDeg.ToString)
                    Sensor_OK = False
                End If
            End If
        End If

        ODrive.Motor(MotorNo).Ready = False
        DriveError = Get_Error(DrvID, MotorID)
        If DriveError > 0 Then
            Debug.WriteLine("ERROR: " & Motor(MotorNo).Text & " (" & MotorNo.ToString & ")")
            Return False
        End If
        If DriveError < 0 Then
            Debug.WriteLine("ERROR: Drive Offline (" & DriveError.ToString & ")")
            Return False
        End If

        ' --- odrive calibration parameter init
        Debug.WriteLine("Calibration parameter update (Profile ID:" & AxisParam.CalibrationProfile.ToString & ")")
        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
        If ODrive.PositionUnit = ODrive.Unit.Pulse Then
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(AxisParam.CalibrationProfile).Velocity_Limit, ODrive.Axis(MotorNo).PPR), True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(AxisParam.CalibrationProfile).Trajectory_Velocity_Limit, ODrive.Axis(MotorNo).PPR), True)
        End If
        If ODrive.PositionUnit = ODrive.Unit.Turn Then
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.MotorPreset(AxisParam.CalibrationProfile).Velocity_Limit, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(AxisParam.CalibrationProfile).Trajectory_Velocity_Limit, True)
        End If
        SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.MotorPreset(AxisParam.CalibrationProfile).Acceleration_Limit, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.MotorPreset(AxisParam.CalibrationProfile).Deacceleration_Limit, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(AxisParam.CalibrationProfile).Position_Gain, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(AxisParam.CalibrationProfile).Velocity_Gain, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(AxisParam.CalibrationProfile).Velocity_Integrator_Gain, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, ODrive.MotorPreset(AxisParam.CalibrationProfile).Calibration_Velocity, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, ODrive.MotorPreset(AxisParam.CalibrationProfile).Calibration_Acceleration, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, ODrive.MotorPreset(AxisParam.CalibrationProfile).Calibration_Ramp, True)
        'Debug.WriteLine("UPDRes " & MotorResult.ToString & " , " & SetResult.ToString & " , " & Sensor_OK)

        If Sensor_OK = False And AxisParam.HomeEnable = True Then
            MotorEvent(DrvID, MotorID, "ERROR: encoder")
            Return False
        End If

        If AxisParam.EndstopEnable = False And AxisParam.HomeEnable = False Then Debug.WriteLine(" --- ! WARNING ! --- IndexOnly MODE (Encoder offset value is ignore and can cause PHYSICAL DAMAGE)")

        ' --- parameter init error trap
        If SetResult > 0 Then
            MotorEvent(DrvID, MotorID, "ERROR: COMM")
            Return False
        End If
        If MotorResult > 0 Then
            Select Case Get_Error(DrvID, MotorID)
                Case > 0 : MotorEvent(DrvID, MotorID, "ERROR: " & ODrive.Motor(MotorNo).ErrText)
                Case < 0 : MotorEvent(DrvID, MotorID, "ERROR: drive offline")
                Case = 0 : MotorEvent(DrvID, MotorID, "ERROR: unknown")
            End Select
            Return False
        End If

        ' ---- check if encoder is ready
        MotorEvent(DrvID, MotorID, "motor encoder check")
        IORegister(DrvID, Reg.IO_HEAD) = 0
        For i As Integer = 0 To 3
            CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_IS_READY))
            Task.Delay(100).Wait()
            If IORegister(DrvID, Reg.IO_HEAD) > 0 Then
                ENCODER_READY = True
                Exit For
            End If
        Next

        ' ---- if encoder is not ready -> do index search
        If (Not ENCODER_READY) And AxisParam.IndexEnable Then
            MotorEvent(DrvID, MotorID, "index")
            IORegister(DrvID, Reg.IO_HEAD) = 0
            MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_ENCODER_INDEX_SEARCH)
            Task.Delay(100).Wait()
            IORegister(DrvID, Reg.IO_HEAD) = 0
            StartTick = System.Environment.TickCount
            EndTick = StartTick + (LocalIndexTimeout * 1000)
            While System.Environment.TickCount < EndTick
                CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_IS_READY))
                Task.Delay(100).Wait()

                ' ---- exit if reach endstop while index
                CommandProcessor("[0]DRIVE(4).WRITE(H)")
                Select Case SensorNo
                    Case 0 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 0)
                    Case 1 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 1)
                    Case 2 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 2)
                    Case 3 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 3)
                    Case Else
                        Sensor_Value = 99
                        MotorEvent(DrvID, MotorID, "endstop config error")
                End Select
                If Sensor_Value > 0 Then
                    MotorEvent(DrvID, MotorID, "index stop : reach endstop position")
                    SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
                    Return False
                End If

                If IORegister(DrvID, Reg.IO_HEAD) > 0 Then
                    IORegister(DrvID, Reg.IO_HEAD) = 0
                    For j As Integer = 0 To 100
                        CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.CURRENT_STATE))
                        Task.Delay(100).Wait()
                        If IORegister(DrvID, Reg.IO_HEAD) = ODrive.AXIS_STATE_IDLE Then
                            ENCODER_READY = True
                            Exit For
                        End If
                    Next
                    Exit While
                End If
            End While
        End If
        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
        If MotorResult > 0 Then
            If Get_Error(DrvID, MotorID) > 0 Then MotorEvent(DrvID, MotorID, ODrive.Motor(MotorNo).ErrText)
            If Get_Error(DrvID, MotorID) < 0 Then MotorEvent(DrvID, MotorID, "drive offline")
            If Get_Error(DrvID, MotorID) = 0 Then MotorEvent(DrvID, MotorID, "unknown")
            Return False
        End If
        Task.Delay(STATE_WAIT).Wait()

        ' ---- if encoder is not ready -> do reverse index search
        If (Not ENCODER_READY) And AxisParam.IndexEnable Then
            MotorEvent(DrvID, MotorID, "reverse index")
            ReverseFlag *= -1.0
            SetResult = 0
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, -40.0, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, -20.0, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, -3.141593, True)

            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, ODrive.MotorPreset(PresetCAL).Calibration_Velocity * -1.0, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, ODrive.MotorPreset(PresetCAL).Calibration_Acceleration * -1.0, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, ODrive.MotorPreset(AxisParam.CalibrationProfile).Calibration_Ramp * ReverseFlag, True)
            Task.Delay(100).Wait()
            MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_ENCODER_INDEX_SEARCH)
            Task.Delay(100).Wait()
            IORegister(DrvID, Reg.IO_HEAD) = 0
            StartTick = System.Environment.TickCount
            EndTick = StartTick + (LocalIndexTimeout * 1000)
            While System.Environment.TickCount < EndTick
                CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_IS_READY))
                Task.Delay(100).Wait()

                ' ---- exit if reach endstop while index
                CommandProcessor("[0]DRIVE(4).WRITE(H)")
                Select Case SensorNo
                    Case 0 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 0)
                    Case 1 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 1)
                    Case 2 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 2)
                    Case 3 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 3)
                    Case Else
                        Sensor_Value = 99
                        MotorEvent(DrvID, MotorID, "endstop config error")
                End Select
                If Sensor_Value > 0 Then
                    MotorEvent(DrvID, MotorID, "index stop : reach endstop position")
                    SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
                    Return False
                End If

                If IORegister(DrvID, Reg.IO_HEAD) > 0 Then
                    IORegister(DrvID, Reg.IO_HEAD) = 0
                    For j As Integer = 0 To 10
                        CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.CURRENT_STATE))
                        Task.Delay(100).Wait()
                        If IORegister(DrvID, Reg.IO_HEAD) = ODrive.AXIS_STATE_IDLE Then
                            ENCODER_READY = True
                            Exit For
                        End If
                    Next
                    Exit While
                End If
            End While
            MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
        End If
        If MotorResult > 0 Then
            If Get_Error(DrvID, MotorID) > 0 Then MotorEvent(DrvID, MotorID, ODrive.Motor(MotorNo).ErrText)
            If Get_Error(DrvID, MotorID) < 0 Then MotorEvent(DrvID, MotorID, "drive offline")
            If Get_Error(DrvID, MotorID) = 0 Then MotorEvent(DrvID, MotorID, "unknown")
            Return False
        End If

        ' ---- if encoder is not ready and (index disable) -> do encoder calibrate and use encoder offser as index
        If (Not ENCODER_READY) And (Not AxisParam.IndexEnable) Then
            MotorEvent(DrvID, MotorID, "calibrate encoder offset")
            IORegister(DrvID, Reg.IO_HEAD) = 0
            MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_ENCODER_OFFSET_CALIBRATION)
            Task.Delay(100).Wait()
            IORegister(DrvID, Reg.IO_HEAD) = 0
            StartTick = System.Environment.TickCount
            EndTick = StartTick + (LocalIndexTimeout * 1000)
            While System.Environment.TickCount < EndTick
                CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_IS_READY))
                Task.Delay(100).Wait()
                If IORegister(DrvID, Reg.IO_HEAD) > 0 Then
                    IORegister(DrvID, Reg.IO_HEAD) = 0
                    For j As Integer = 0 To 100
                        CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.CURRENT_STATE))
                        Task.Delay(100).Wait()
                        If IORegister(DrvID, Reg.IO_HEAD) = ODrive.AXIS_STATE_IDLE Then
                            ENCODER_READY = True
                            Exit For
                        End If
                    Next
                    Exit While
                End If
            End While
            MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
        End If
        If MotorResult > 0 Then
            If Get_Error(DrvID, MotorID) > 0 Then MotorEvent(DrvID, MotorID, ODrive.Motor(MotorNo).ErrText)
            If Get_Error(DrvID, MotorID) < 0 Then MotorEvent(DrvID, MotorID, "drive offline")
            If Get_Error(DrvID, MotorID) = 0 Then MotorEvent(DrvID, MotorID, "unknown")
            Return False
        End If

        If ENCODER_READY Then
            For i As Integer = 1 To 6
                CommandProcessor("[0]DRIVE(4).WRITE(E)")
                Task.Delay(250).Wait()
            Next
            Select Case MotorNo
                Case 0 : Sensor_Value = IORegister(Reg.Device_SystemIO, Reg.IO_X)
                Case 1 : Sensor_Value = IORegister(Reg.Device_SystemIO, Reg.IO_Y)
                Case 2 : Sensor_Value = IORegister(Reg.Device_SystemIO, Reg.IO_Z)
                Case 3 : Sensor_Value = IORegister(Reg.Device_SystemIO, Reg.IO_A)
            End Select

            If Sensor_Value > 3000.0 Then Sensor_Value = Sensor_Value - 3000.0 '---- IGNORE MAGNETIC FIELD ERROR ----
            If Sensor_Value > 2000.0 Then Sensor_Value = Sensor_Value - 2000.0 '---- IGNORE MAGNETIC FIELD ERROR ----
            If Sensor_Value > 1000.0 Then Sensor_Value = Sensor_Value - 1000.0 '---- IGNORE MAGNETIC FIELD ERROR ----
            If Sensor_Value > 360.0 Then Sensor_OK = False
            If Sensor_Value < 0.1 Then Sensor_OK = False
        Else
            MotorEvent(DrvID, MotorID, "ERROR: index failed")
            Result = False
        End If

        ' --- Move Home : Absolute Encoder
        If ENCODER_READY And AxisParam.IndexEnable And AxisParam.HomeEnable And AxisParam.EndstopEnable = False Then
            If Sensor_OK Then
                MotorEvent(DrvID, MotorID, "move home")
                CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
                Task.Delay(CMD_WAIT).Wait()
                CurrentPOS = IORegister(DrvID, Reg.IO_HEAD)
                Sensor_Diff = AxisMath.GetCircleShortPath(Sensor_Value, AxisParam.HomeDeg)
                Move_Request = CurrentPOS + ((((AxisParam.PPR * AxisParam.GearRatio) / 360.0) * Sensor_Diff) * EncDir)
                Debug.WriteLine("[ENCODER] Position:" & Sensor_Value & " Target:" & AxisParam.HomeDeg & " Diff:" & Sensor_Diff)
                Debug.WriteLine("[MOTOR] Position:" & CurrentPOS & " Target:" & Move_Request & " Diff:" & Move_Request - CurrentPOS)
                If Math.Abs(IORegister(DrvID, Reg.IO_HEAD) - Move_Request) < LocalHomeTotalance Then
                    MOVE_OK = True
                Else
                    MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_CLOSED_LOOP_CONTROL)
                    Task.Delay(STATE_WAIT).Wait()
                    CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID & " " & Move_Request.ToString & ")")
                    For i As Integer = 0 To LocalHomeTimeout Step 100
                        CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
                        Task.Delay(100).Wait()
                        If Math.Abs(IORegister(DrvID, Reg.IO_HEAD) - Move_Request) < LocalHomeTotalance Then
                            MOVE_OK = True
                            Task.Delay(STATE_WAIT).Wait()
                            Exit For
                        End If
                    Next
                    MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
                End If
                Task.Delay(STATE_WAIT).Wait()

                If MOVE_OK Then
                    MotorEvent(DrvID, MotorID, "get motor offset")
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
                    Task.Delay(CMD_WAIT).Wait()
                    CurrentPOS = IORegister(DrvID, Reg.IO_HEAD)
                    Select Case MotorNo
                        Case 0 : ControlRegister(Register.Axis0_Offset) = CurrentPOS
                        Case 1 : ControlRegister(Register.Axis1_Offset) = CurrentPOS
                        Case 2 : ControlRegister(Register.Axis2_Offset) = CurrentPOS
                        Case 3 : ControlRegister(Register.Axis3_Offset) = CurrentPOS
                        Case 4 : ControlRegister(Register.Axis4_Offset) = CurrentPOS
                        Case 5 : ControlRegister(Register.Axis5_Offset) = CurrentPOS
                    End Select

                    If ODrive.PositionUnit = ODrive.Unit.Pulse Then
                        SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(AxisParam.RunProfile).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
                        SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(AxisParam.RunProfile).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
                    End If
                    If ODrive.PositionUnit = ODrive.Unit.Turn Then
                        SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.MotorPreset(AxisParam.RunProfile).Safe_Velocity, True)
                        SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(AxisParam.RunProfile).Safe_Velocity, True)
                    End If

                    SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.MotorPreset(AxisParam.RunProfile).Acceleration_Limit, True)
                    SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.MotorPreset(AxisParam.RunProfile).Deacceleration_Limit, True)
                    SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(AxisParam.RunProfile).Position_Gain, True)
                    SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(AxisParam.RunProfile).Velocity_Gain, True)
                    SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(AxisParam.RunProfile).Velocity_Integrator_Gain, True)
                    SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, ODrive.MotorPreset(AxisParam.RunProfile).Calibration_Velocity, True)
                    SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, ODrive.MotorPreset(AxisParam.RunProfile).Calibration_Acceleration, True)
                    SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, ODrive.MotorPreset(AxisParam.RunProfile).Calibration_Ramp, True)
                    Result = True
                Else
                    MotorEvent(DrvID, MotorID, "move error")
                    Result = False
                End If

            End If

        End If

        ' --- Move Home : End stop sensor
        If ENCODER_READY And AxisParam.IndexEnable And AxisParam.HomeEnable = False And AxisParam.EndstopEnable Then
            Debug.WriteLine("ESO:" & AxisParam.EndstopOffset.ToString & " " & SensorNo.ToString)
            MotorEvent(DrvID, MotorID, "get index position")
            'Debug.WriteLine("That's no moon. It's a trap.")
            'Return False

            For i As Integer = 1 To 3
                CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
                Task.Delay(CMD_WAIT).Wait()
            Next

            ' if encoder position is not center -> force index search
            If Math.Abs(IORegister(DrvID, Reg.IO_HEAD)) > 0.15 Then
                Debug.WriteLine("force index search")
                ENCODER_READY = False
                MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
                Task.Delay(250).Wait()
                MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_ENCODER_INDEX_SEARCH)
                Task.Delay(250).Wait()
                For j As Integer = 0 To 100
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.CURRENT_STATE))
                    Task.Delay(100).Wait()
                    If IORegister(DrvID, Reg.IO_HEAD) = ODrive.AXIS_STATE_IDLE Then
                        ENCODER_READY = True
                        Exit For
                    End If
                Next
            End If

            'Debug.WriteLine("That is no moon")
            'Return False
            If ENCODER_READY Then
                MOVE_OK = False
                Task.Delay(STATE_WAIT).Wait()
                MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_CLOSED_LOOP_CONTROL)
                Task.Delay(STATE_WAIT).Wait()
                Dim hPOS As Single = 0.0
                Dim hOffset As Single = AxisParam.EndstopOffset ' 12.0
                Dim xtimeout As Integer = 10 * 1000 'sec
                ' --- move to home sensor
                For ii As Integer = 0 To xtimeout
                    Task.Delay(25).Wait()
                    hPOS -= 0.025
                    CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & hPOS.ToString & ")")
                    Task.Delay(25).Wait()
                    CommandProcessor("[0]DRIVE(4).WRITE(H)")
                    Select Case SensorNo
                        Case 0 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 0)
                        Case 1 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 1)
                        Case 2 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 2)
                        Case 3 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 3)
                    End Select
                    If Sensor_Value > 0 Then Exit For
                Next
                Task.Delay(1000).Wait()
                ' --- move out of home sensor
                For ii As Integer = 0 To 300
                    Task.Delay(50).Wait()
                    hPOS += 0.01
                    CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & hPOS.ToString & ")")
                    Task.Delay(50).Wait()
                    CommandProcessor("[0]DRIVE(4).WRITE(H)")
                    Select Case SensorNo
                        Case 0 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 0)
                        Case 1 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 1)
                        Case 2 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 2)
                        Case 3 : Sensor_Value = getBit(IORegister(Reg.Device_SystemIO, Reg.IO_H), 3)
                    End Select
                    If Sensor_Value = 0 Then Exit For
                Next
                ' --- move to home position
                Task.Delay(100).Wait()
                CommandProcessor("[0]drive(" & DrvID.ToString & ").write(t " & MotorID.ToString & " " & (hPOS + hOffset).ToString & ")")
                For j As Integer = 0 To 300
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
                    Task.Delay(100).Wait()
                    If Math.Abs(IORegister(DrvID, Reg.IO_HEAD) - (hPOS + hOffset)) < 0.015 Then Exit For
                Next
                Task.Delay(1000).Wait()
                MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
                ' --- init encoder index
                Task.Delay(3000).Wait()
                MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_ENCODER_INDEX_SEARCH)
                Task.Delay(250).Wait()
                For j As Integer = 0 To 100
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.CURRENT_STATE))
                    Task.Delay(100).Wait()
                    If IORegister(DrvID, Reg.IO_HEAD) = ODrive.AXIS_STATE_IDLE Then
                        MOVE_OK = True
                        Exit For
                    End If
                Next
                Debug.WriteLine("END")
            End If

            If MOVE_OK Then
                MotorEvent(DrvID, MotorID, "get motor offset")
                CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
                Task.Delay(CMD_WAIT).Wait()
                CurrentPOS = IORegister(DrvID, Reg.IO_HEAD)
                Select Case MotorNo
                    Case 0 : ControlRegister(Register.Axis0_Offset) = CurrentPOS
                    Case 1 : ControlRegister(Register.Axis1_Offset) = CurrentPOS
                    Case 2 : ControlRegister(Register.Axis2_Offset) = CurrentPOS
                    Case 3 : ControlRegister(Register.Axis3_Offset) = CurrentPOS
                    Case 4 : ControlRegister(Register.Axis4_Offset) = CurrentPOS
                    Case 5 : ControlRegister(Register.Axis5_Offset) = CurrentPOS
                End Select

                If ODrive.PositionUnit = ODrive.Unit.Pulse Then
                    SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(AxisParam.RunProfile).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
                    SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(AxisParam.RunProfile).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
                End If
                If ODrive.PositionUnit = ODrive.Unit.Turn Then
                    SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.MotorPreset(AxisParam.RunProfile).Safe_Velocity, True)
                    SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(AxisParam.RunProfile).Safe_Velocity, True)
                End If

                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.MotorPreset(AxisParam.RunProfile).Acceleration_Limit, True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.MotorPreset(AxisParam.RunProfile).Deacceleration_Limit, True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(AxisParam.RunProfile).Position_Gain, True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(AxisParam.RunProfile).Velocity_Gain, True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(AxisParam.RunProfile).Velocity_Integrator_Gain, True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, ODrive.MotorPreset(AxisParam.RunProfile).Calibration_Velocity, True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, ODrive.MotorPreset(AxisParam.RunProfile).Calibration_Acceleration, True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, ODrive.MotorPreset(AxisParam.RunProfile).Calibration_Ramp, True)
                Result = True
            Else
                MotorEvent(DrvID, MotorID, "move error")
                Result = False
            End If

        End If
        'Debug.WriteLine("it's a trap !")
        'Return False


        ' --- assume current position as home (no home attemp, ignore index)
        If ENCODER_READY = True And AxisParam.IndexEnable = True And AxisParam.HomeEnable = False And AxisParam.EndstopEnable = False Then
            MotorEvent(DrvID, MotorID, "get motor offset")
            CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
            Task.Delay(CMD_WAIT).Wait()
            CurrentPOS = IORegister(DrvID, Reg.IO_HEAD)
            Select Case MotorNo
                Case 0 : ControlRegister(Register.Axis0_Offset) = CurrentPOS
                Case 1 : ControlRegister(Register.Axis1_Offset) = CurrentPOS
                Case 2 : ControlRegister(Register.Axis2_Offset) = CurrentPOS
                Case 3 : ControlRegister(Register.Axis3_Offset) = CurrentPOS
                Case 4 : ControlRegister(Register.Axis4_Offset) = CurrentPOS
                Case 5 : ControlRegister(Register.Axis5_Offset) = CurrentPOS
            End Select
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_CURRENT, 10.0, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIM, ODrive.MotorPreset(PresetID).Current_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIMIT_TOLERANCE, ODrive.MotorPreset(PresetID).Current_Limit_Tolerance, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.REQUESTED_CURRENT_RANGE, ODrive.MotorPreset(PresetID).Current_Range, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_CONTROL_BANDWIDTH, 500.0, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.SlowPreset(PresetID).Velocity_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.SlowPreset(PresetID).Velocity_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.SlowPreset(PresetID).Acceleration_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.SlowPreset(PresetID).Deacceleration_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(PresetID).Position_Gain, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(PresetID).Velocity_Gain, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(PresetID).Velocity_Integrator_Gain, True)

            'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_CURRENT, ODrive.MotorPreset(PresetRUN).Calibration_Current, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIM, ODrive.MotorPreset(PresetRUN).Current_Limit, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIMIT_TOLERANCE, ODrive.MotorPreset(PresetRUN).Current_Limit_Tolerance, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.REQUESTED_CURRENT_RANGE, ODrive.MotorPreset(PresetRUN).Current_Range, True)
            'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_CONTROL_BANDWIDTH, ODrive.MotorPreset(PresetRUN).Current_Control_Bandwidth, True)

            If ODrive.PositionUnit = ODrive.Unit.Pulse Then
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(AxisParam.RunProfile).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(AxisParam.RunProfile).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
            End If
            If ODrive.PositionUnit = ODrive.Unit.Turn Then
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.MotorPreset(AxisParam.RunProfile).Safe_Velocity, True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(AxisParam.RunProfile).Safe_Velocity, True)
            End If

            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.MotorPreset(AxisParam.RunProfile).Acceleration_Limit, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.MotorPreset(AxisParam.RunProfile).Deacceleration_Limit, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(AxisParam.RunProfile).Position_Gain, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(AxisParam.RunProfile).Velocity_Gain, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(AxisParam.RunProfile).Velocity_Integrator_Gain, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_VELOCITY, ODrive.MotorPreset(AxisParam.RunProfile).Calibration_Velocity, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_ACCEL, ODrive.MotorPreset(AxisParam.RunProfile).Calibration_Acceleration, True)
            SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_RAMP, ODrive.MotorPreset(AxisParam.RunProfile).Calibration_Ramp, True)
            Result = True
        End If

        ' turn off motor befor leave
        MotorResult += SetMotorState(DrvID, MotorID, ODrive.AXIS_STATE_IDLE)
        MotorEvent(DrvID, MotorID, "motor idle")

        If SetResult > 0 Then
            MotorEvent(DrvID, MotorID, "COMM ERROR")
            Result = False
        End If
        If MotorResult > 0 Then
            Dim TmpErrStr As String = ""
            If Get_Error(DrvID, MotorID) > 0 Then
                MotorEvent(DrvID, MotorID, ODrive.Motor(MotorNo).ErrText)
            Else
                MotorEvent(DrvID, MotorID, "DRV ERROR")
            End If
            Result = False
        End If

        If Result Then MotorEvent(DrvID, MotorID, "ready")
        Debug.WriteLine("finish ( T = " & (System.Environment.TickCount - TimeStamp).ToString & " ) ( OFFSET = " & CurrentPOS & " )")

        Return Result
    End Function

    Function ForceProfileUpdate(DrvID As Integer, MotorID As Integer, PresetID As Integer, SafeMode As Boolean) As Integer

        'Dim STATE_Wait As Integer = 250
        'Dim CMD_Wait As Integer = 50
        Dim SetResult As Integer = 0
        Dim MotorNo As Integer = -1
        Dim STATE_WAIT As Integer = OPConfig.Delay_StateChange
        Dim CMD_WAIT As Integer = OPConfig.Delay_ParameterSet

        For i As Integer = 0 To 2
            CommandProcessor(ODrive.WriteCommand(DrvID, MotorID, ODrive.REQUEST_STATE, ODrive.AXIS_STATE_IDLE))
            Task.Delay(CMD_WAIT).Wait()
        Next
        Task.Delay(STATE_WAIT).Wait()
        If DrvID >= 0 And MotorID >= 0 Then
            For i As Integer = 0 To 3
                If DrvID = ODrive.Map_DRV(i) And MotorID = ODrive.Map_OUT(i) Then
                    MotorNo = i
                    Exit For
                End If
            Next
        End If
        If MotorNo < 0 Then
            Debug.WriteLine("ERROR:Invalid Mapping [" & MotorNo.ToString & "] DRV:" & DrvID.ToString & " OUT:" & MotorID.ToString)
            Return 1
        End If

        'SetResult += SetParameter(DrvID, MotorID, ODrive.CALIBRATION_CURRENT, ODrive.MotorPreset(PresetID).Calibration_Current, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_LIM, ODrive.MotorPreset(PresetID).Current_Limit, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.REQUESTED_CURRENT_RANGE, ODrive.MotorPreset(PresetID).Current_Range, True)
        'SetResult += SetParameter(DrvID, MotorID, ODrive.CURRENT_CONTROL_BANDWIDTH, ODrive.MotorPreset(PresetID).Current_Control_Bandwidth, True)
        If SafeMode Then
            If ODrive.PositionUnit = ODrive.Unit.Pulse Then
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetID).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetID).Safe_Velocity, ODrive.Axis(MotorNo).PPR), True)
            End If
            If ODrive.PositionUnit = ODrive.Unit.Turn Then
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.MotorPreset(PresetID).Safe_Velocity, True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(PresetID).Safe_Velocity, True)
            End If
        Else
            If ODrive.PositionUnit = ODrive.Unit.Pulse Then
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetID).Velocity_Limit, ODrive.Axis(MotorNo).PPR), True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(PresetID).Trajectory_Velocity_Limit, ODrive.Axis(MotorNo).PPR), True)
            End If
            If ODrive.PositionUnit = ODrive.Unit.Turn Then
                SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_LIMIT, ODrive.MotorPreset(PresetID).Velocity_Limit, True)
                SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(PresetID).Trajectory_Velocity_Limit, True)
            End If
        End If
        SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_ACCEL_LIMIT, ODrive.MotorPreset(PresetID).Acceleration_Limit, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.TRAJ_DECEL_LIMIT, ODrive.MotorPreset(PresetID).Deacceleration_Limit, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.POS_GAIN, ODrive.MotorPreset(PresetID).Position_Gain, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_GAIN, ODrive.MotorPreset(PresetID).Velocity_Gain, True)
        SetResult += SetParameter(DrvID, MotorID, ODrive.VEL_INTEGRATOR_GAIN, ODrive.MotorPreset(PresetID).Velocity_Integrator_Gain, True)

        If SetResult > 0 Then MotorEvent(DrvID, MotorID, "ERROR:COMM")

        Return SetResult
    End Function

    Sub MotorEvent(DrvID As Integer, MotorID As Integer, EventStr As String)
        Const DisplayPageID As Integer = 1
        Dim NTextName As String = ""
        Dim MotorNo As Integer = -1

        If DrvID >= 0 And MotorID >= 0 Then
            For i As Integer = 0 To 7
                'Debug.WriteLine("AxNo:" & DrvID.ToString & "," & MotorID.ToString & "," & i.ToString)
                If DrvID = ODrive.Map_DRV(i) And MotorID = ODrive.Map_OUT(i) Then
                    MotorNo = i
                    Log.Data.Axis_StateTXT(i) = EventStr
                    Exit For
                End If
            Next
        End If

        Select Case MotorNo
            Case 0 : NTextName = "page_status.ms0"
            Case 1 : NTextName = "page_status.ms1"
            Case 2 : NTextName = "page_status.ms2"
            Case 3 : NTextName = "page_status.ms3"
        End Select

        If MotorNo > -1 Then
            ODrive.Motor(MotorNo).Text = EventStr
            If IORegister(3, Reg.IO_P) = DisplayPageID Then WriteNextion(NTextName & ".txt=""" & ODrive.Motor(MotorNo).Text & """")
        End If
        Debug.WriteLine("EVENT - DRV:" & DrvID.ToString & " OUT:" & MotorID.ToString & " Axis:" & MotorNo.ToString & " [" & EventStr & "]")
    End Sub

    Function SetParameter(DrvID As Integer, MotorID As Integer, ParamID As Integer, ParamValue As Single, Verlify As Boolean) As Integer
        Dim Res As Integer = 1
        'Dim ResWait As Integer = 50
        Dim ScriptStatus As Boolean = False
        Dim STATE_WAIT As Integer = OPConfig.Delay_StateChange
        Dim CMD_WAIT As Integer = OPConfig.Delay_ParameterSet

        'Return 0

        If Script_RUN Then ScriptStatus = True
        Script_RUN = True
        If Verlify Then
            For i As Integer = 0 To 10
                CommandProcessor(ODrive.WriteCommand(DrvID, MotorID, ParamID, ParamValue))
                'Debug.WriteLine(ODrive.WriteCommand(DrvID, MotorID, ParamID, ParamValue))
                Task.Delay(CMD_WAIT).Wait()
                CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ParamID))
                For j As Integer = 0 To 5
                    Task.Delay(CMD_WAIT).Wait()
                    If Math.Abs(IORegister(DrvID, Reg.IO_HEAD) - ParamValue) < 0.1 Then
                        Res = 0
                        Exit For
                    End If
                Next
                If Res = 0 Then Exit For
            Next
        Else
            CommandProcessor(ODrive.WriteCommand(DrvID, MotorID, ParamID, ParamValue))
            Res = 0
        End If
        If Res > 0 Then SysEvent("Parameter Set Error : Drv[" & DrvID.ToString & "] Motor[" & MotorID.ToString & "] CMD[" & ODrive.WriteCommand(DrvID, MotorID, ParamID, ParamValue) & "] Res[" & IORegister(DrvID, Reg.IO_HEAD).ToString & "]")
        If ScriptStatus = False Then Script_RUN = False

        Return Res
    End Function

    Function SetMotorState(DrvID As Integer, MotorID As Integer, StateValue As Integer) As Integer

        Dim Res As Integer = 1
        Dim StateWait As Integer = 100
        Dim ResWait As Integer = 20
        Dim ScriptStatus As Boolean = False
        Dim SafeStart_Cnt As Integer = 0

        'Return 0

        If Script_RUN Then ScriptStatus = True
        Script_RUN = True

        SysEvent("Motor State Request : " & StateValue)
        If StateValue = ODrive.AXIS_STATE_CLOSED_LOOP_CONTROL Then
            SysEvent("Encoder Position Check")
            Task.Delay(StateWait).Wait()
            For i As Integer = 0 To 10
                CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_POS_ESTIMATE))
                Task.Delay(StateWait).Wait()
                If Math.Abs(IORegister(DrvID, Reg.IO_HEAD)) < 0.2 Then
                    SafeStart_Cnt += 1
                End If
            Next
        Else
            SafeStart_Cnt = 10
        End If

        If SafeStart_Cnt >= 5 Then
            For i As Integer = 0 To 10
                Task.Delay(StateWait).Wait()
                CommandProcessor(ODrive.WriteCommand(DrvID, MotorID, ODrive.REQUEST_STATE, StateValue))
                Task.Delay(StateWait).Wait()
                CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.CURRENT_STATE))
                For j As Integer = 0 To 3
                    Task.Delay(ResWait).Wait()
                    If StateValue = ODrive.AXIS_STATE_IDLE And Math.Truncate(IORegister(DrvID, Reg.IO_HEAD)) = ODrive.AXIS_STATE_IDLE Then
                        Res = 0
                        Exit For
                    End If
                    If StateValue > ODrive.AXIS_STATE_IDLE And Math.Truncate(IORegister(DrvID, Reg.IO_HEAD)) > ODrive.AXIS_STATE_IDLE Then
                        Res = 0
                        Exit For
                    End If
                Next
                If Res = 0 Then Exit For
            Next
        Else
            SysEvent("Safety Violation : motor start position not zero")
        End If

        If ScriptStatus = False Then Script_RUN = False

        Return Res
    End Function

    Function Get_Error(DrvID As Integer, MotorID As Integer) As Integer

        'Dim DRV_VOLTAGE As Single = 0.0
        Dim GEN_ERR As Integer = 0
        Dim MOT_ERR As Integer = 0
        Dim ENC_ERR As Integer = 0
        Dim ERR_CODE As Integer = -1
        Dim MotorNo As Integer = -1

        If DrvID = ODrive.Map_DRV(0) And MotorID = ODrive.Map_OUT(0) Then MotorNo = 0
        If DrvID = ODrive.Map_DRV(1) And MotorID = ODrive.Map_OUT(1) Then MotorNo = 1
        If DrvID = ODrive.Map_DRV(2) And MotorID = ODrive.Map_OUT(2) Then MotorNo = 2
        If DrvID = ODrive.Map_DRV(3) And MotorID = ODrive.Map_OUT(3) Then MotorNo = 3
        If DrvID = ODrive.Map_DRV(4) And MotorID = ODrive.Map_OUT(4) Then MotorNo = 4
        If DrvID = ODrive.Map_DRV(5) And MotorID = ODrive.Map_OUT(5) Then MotorNo = 5
        If DrvID = ODrive.Map_DRV(6) And MotorID = ODrive.Map_OUT(6) Then MotorNo = 6
        If DrvID = ODrive.Map_DRV(7) And MotorID = ODrive.Map_OUT(7) Then MotorNo = 7
        If DrvID < 0 Or MotorID < 0 Then MotorNo = -1

        Debug.WriteLine("check DRV:" & DrvID.ToString & " OUT:" & MotorID.ToString & " Axis:" & MotorNo.ToString)

        If MotorNo < 0 Or DrvID < 0 Or MotorID < 0 Then Return -1

        ODrive.Motor(MotorNo).ErrText = ""

        ERR_CODE = 0
        IORegister(DrvID, Reg.IO_HEAD) = -1.0
        For i As Integer = 0 To 10
            CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.GENERAL_ERROR))
            Task.Delay(50).Wait()
            GEN_ERR = Math.Truncate(IORegister(DrvID, Reg.IO_HEAD))
            If GEN_ERR >= 0.0 Then
                Exit For
            End If
        Next
        Select Case GEN_ERR
            Case 64
                IORegister(DrvID, Reg.IO_HEAD) = -1.0
                For i As Integer = 0 To 10
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.MOTOR_ERROR))
                    Task.Delay(50).Wait()
                    MOT_ERR = Math.Truncate(IORegister(DrvID, Reg.IO_HEAD))
                    If MOT_ERR >= 0.0 Then Exit For
                Next
            Case 256
                IORegister(DrvID, Reg.IO_HEAD) = -1.0
                For i As Integer = 0 To 10
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_ERROR))
                    Task.Delay(50).Wait()
                    ENC_ERR = Math.Truncate(IORegister(DrvID, Reg.IO_HEAD))
                    If ENC_ERR >= 0.0 Then Exit For
                Next
            Case Else
                IORegister(DrvID, Reg.IO_HEAD) = -1.0
                For i As Integer = 0 To 10
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.MOTOR_ERROR))
                    Task.Delay(50).Wait()
                    MOT_ERR = Math.Truncate(IORegister(DrvID, Reg.IO_HEAD))
                    If MOT_ERR >= 0.0 Then Exit For
                Next
                IORegister(DrvID, Reg.IO_HEAD) = -1.0
                For i As Integer = 0 To 10
                    CommandProcessor(ODrive.ReadCommand(DrvID, MotorID, ODrive.ENCODER_ERROR))
                    Task.Delay(50).Wait()
                    ENC_ERR = Math.Truncate(IORegister(DrvID, Reg.IO_HEAD))
                    If ENC_ERR >= 0.0 Then Exit For
                Next
        End Select

        If GEN_ERR > 0 Then
            ERR_CODE = GEN_ERR
            ODrive.Motor(MotorNo).ErrText = "Error " & ODrive.GeneralError(GEN_ERR)
        End If
        If MOT_ERR > 0 Then
            ERR_CODE = MOT_ERR
            ODrive.Motor(MotorNo).ErrText = ODrive.Motor(MotorNo).ErrText & ":" & ODrive.MotorError(MOT_ERR)
        End If
        If ENC_ERR > 0 Then
            ERR_CODE = ENC_ERR
            ODrive.Motor(MotorNo).ErrText = ODrive.Motor(MotorNo).ErrText & ":" & ODrive.EncoderError(ENC_ERR)
        End If
        Debug.WriteLine(ODrive.Motor(MotorNo).ErrText)
        If ERR_CODE < 0 Or ERR_CODE < 0 Then
            ERR_CODE = -1
        End If

        Return ERR_CODE
    End Function

    Sub MotorOutputControl(MotorState As Integer, MotorSpeedProfile As Integer, ParkEnable As Boolean)
        Dim AutoTelemetry As Boolean = True
        Const WaitCMD As Integer = 100
        Dim RQValue As UInt16 = 0
        Dim MState As Integer = 1
        Dim ScriptState As Boolean = False
        Dim Activity As Boolean = False
        If MotorState > 1 Then RQValue = ODrive.AXIS_STATE_CLOSED_LOOP_CONTROL
        If MotorState = 1 Then RQValue = ODrive.AXIS_STATE_IDLE
        Dim SetResult As Integer = 0
        Dim MotorResult As Integer = 0
        Dim Statistics As Storage.Machine_Statistics

        SysEvent("MotorState Request : " & MotorState.ToString & " Profile : " & MotorSpeedProfile.ToString)

        ControlRegister(Register.MotorControl_Busy) = 1

        If MotorState = 1 And AutoTelemetry Then
            TelemetryEnable = False
        End If

        ScriptState = Script_RUN
        If ScriptState = False Then Script_RUN = True

        If MotorState = 1 And ParkEnable Then Motor_Parking()

        For i As Integer = 0 To 2
            Task.Delay(50).Wait()
            For j As Integer = 0 To 3
                If ODrive.Motor(j).Enable And ODrive.Map_DRV(j) >= 0 And ODrive.Map_OUT(j) >= 0 Then
                    CommandProcessor(ODrive.ReadCommand(ODrive.Map_DRV(j), ODrive.Map_OUT(j), ODrive.CURRENT_STATE))
                    Task.Delay(WaitCMD).Wait()
                    If IORegister(ODrive.Map_DRV(j), Reg.IO_HEAD) > ODrive.AXIS_STATE_IDLE Then MState = 1
                    'CommandProcessor(ODrive.ReadCommand(ODrive.Map_DRV(j), ODrive.Map_OUT(j), ODrive.CURRENT_STATE))
                Else
                    ODrive.Motor(j).Enable = False
                End If
            Next
        Next
        If MotorState = AXIS_STATE_IDLE Then
            ODrive.Motor(0).Ready = False
            ODrive.Motor(1).Ready = False
            ODrive.Motor(2).Ready = False
            ODrive.Motor(3).Ready = False
            ODrive.Motor(4).Ready = False
            ODrive.Motor(5).Ready = False
            For j As Integer = 0 To 3
                If ODrive.Motor(j).Enable And ODrive.Map_DRV(j) >= 0 And ODrive.Map_OUT(j) >= 0 Then
                    SetMotorState(ODrive.Map_DRV(j), ODrive.Map_OUT(j), ODrive.AXIS_STATE_IDLE)
                    MotorEvent(ODrive.Map_DRV(j), ODrive.Map_OUT(j), "idle")
                End If
            Next
            MState = 0
        End If
        'Debug.WriteLine(MotorState & " - " & MotorProfile & " - " & MState & " - " & MotorProfile_Shadow)
        If MState = 0 Then
            If MotorProfile_Shadow <> MotorSpeedProfile Then
                MotorProfile_Shadow = MotorSpeedProfile
                If MotorSpeedProfile = 1 Then
                    Debug.WriteLine("normal profile select")
                    For i As Integer = 0 To 3
                        If ODrive.Motor(i).Enable Then
                            Debug.WriteLine("Axis(" & i.ToString & ")profile id:" & ODrive.Axis(i).RunProfile.ToString)
                            SetResult = 0
                            If ODrive.PositionUnit = ODrive.Unit.Pulse Then
                                SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(ODrive.Axis(i).RunProfile).Velocity_Limit, ODrive.Axis(i).PPR), True)
                                SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(ODrive.Axis(i).RunProfile).Trajectory_Velocity_Limit, ODrive.Axis(i).PPR), True)
                            End If
                            If ODrive.PositionUnit = ODrive.Unit.Turn Then
                                SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.VEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).RunProfile).Velocity_Limit, True)
                                SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).RunProfile).Trajectory_Velocity_Limit, True)
                            End If

                            'SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_ACCEL_LIMIT, ODrive.MotorPreset(ODrive.MotorProfile(i)).Acceleration_Limit, True)
                            'SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_DECEL_LIMIT, ODrive.MotorPreset(ODrive.MotorProfile(i)).Deacceleration_Limit, True)
                        End If
                    Next
                Else
                    Debug.WriteLine("slow profile select")
                    For i As Integer = 0 To 3
                        If ODrive.Motor(i).Enable Then
                            Debug.WriteLine("Axis(" & i.ToString & ")profile id:" & ODrive.Axis(i).RunProfile.ToString)
                            SetResult = 0
                            If ODrive.PositionUnit = ODrive.Unit.Pulse Then
                                SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Safe_Velocity, ODrive.Axis(i).PPR), True)
                                SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_VEL_LIMIT, ODrive.RPM_to_Pluse(ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Safe_Velocity, ODrive.Axis(i).PPR), True)
                            End If
                            If ODrive.PositionUnit = ODrive.Unit.Turn Then
                                SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.VEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Safe_Velocity, True)
                                SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_VEL_LIMIT, ODrive.MotorPreset(ODrive.Axis(i).CalibrationProfile).Safe_Velocity, True)
                            End If

                            'SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_ACCEL_LIMIT, ODrive.SlowPreset(ODrive.MotorProfile(i)).Acceleration_Limit, True)
                            'SetResult += SetParameter(ODrive.Map_DRV(i), ODrive.Map_OUT(i), ODrive.TRAJ_DECEL_LIMIT, ODrive.SlowPreset(ODrive.MotorProfile(i)).Deacceleration_Limit, True)
                        End If
                    Next
                End If
                Debug.WriteLine("profile update")
            End If
        End If

        If RQValue <> ODrive.AXIS_STATE_IDLE Then
            Task.Delay(500).Wait() ' ---- need delay after parameter update to prevent motor jumping
            For j As Integer = 0 To 5
                If ODrive.Motor(j).Enable And ODrive.Map_DRV(j) >= 0 And ODrive.Map_OUT(j) >= 0 Then
                    MotorResult = SetMotorState(ODrive.Map_DRV(j), ODrive.Map_OUT(j), RQValue)
                    If MotorResult > 0 Then
                        MotorEvent(ODrive.Map_DRV(j), ODrive.Map_OUT(j), "error")
                        ODrive.Motor(j).Enable = False
                    Else
                        MotorEvent(ODrive.Map_DRV(j), ODrive.Map_OUT(j), "run")
                    End If
                End If
            Next
        End If

        If RQValue = ODrive.AXIS_STATE_IDLE Then
            WriteCommand(Reg.Device_SystemIO, "F111111")
        End If

        If ODrive.PositionUnit = ODrive.Unit.Pulse Then
            If TotalRevolution(0) > 0.0 Then ControlRegister(Register.Axis0_TotalRev) += TotalRevolution(0) / ODrive.Axis(0).PPR
            If TotalRevolution(1) > 0.0 Then ControlRegister(Register.Axis1_TotalRev) += TotalRevolution(1) / ODrive.Axis(1).PPR
            If TotalRevolution(2) > 0.0 Then ControlRegister(Register.Axis2_TotalRev) += TotalRevolution(2) / ODrive.Axis(2).PPR
            If TotalRevolution(3) > 0.0 Then ControlRegister(Register.Axis3_TotalRev) += TotalRevolution(3) / ODrive.Axis(3).PPR
        End If
        If ODrive.PositionUnit = ODrive.Unit.Turn Then
            If TotalRevolution(0) > 0.0 Then ControlRegister(Register.Axis0_TotalRev) += TotalRevolution(0)
            If TotalRevolution(1) > 0.0 Then ControlRegister(Register.Axis1_TotalRev) += TotalRevolution(1)
            If TotalRevolution(2) > 0.0 Then ControlRegister(Register.Axis2_TotalRev) += TotalRevolution(2)
            If TotalRevolution(3) > 0.0 Then ControlRegister(Register.Axis3_TotalRev) += TotalRevolution(3)
        End If


        If TotalTime(0) > 0.0 Then ControlRegister(Register.Axis0_TotalTime) += TotalTime(0) / 1000
        If TotalTime(1) > 0.0 Then ControlRegister(Register.Axis1_TotalTime) += TotalTime(1) / 1000
        If TotalTime(2) > 0.0 Then ControlRegister(Register.Axis2_TotalTime) += TotalTime(2) / 1000
        If TotalTime(3) > 0.0 Then ControlRegister(Register.Axis3_TotalTime) += TotalTime(3) / 1000

        For i As Integer = 0 To 3
            If TotalRevolution(i) > 0.0 Then Activity = True
            If TotalTime(i) > 0.0 Then Activity = True
            TotalRevolution(i) = 0.0
            TotalTime(i) = 0.0
        Next

        If Activity Then
            Statistics.Axis0_Revolution = ControlRegister(Register.Axis0_TotalRev)
            Statistics.Axis1_Revolution = ControlRegister(Register.Axis1_TotalRev)
            Statistics.Axis2_Revolution = ControlRegister(Register.Axis2_TotalRev)
            Statistics.Axis3_Revolution = ControlRegister(Register.Axis3_TotalRev)
            Statistics.Axis0_Time = ControlRegister(Register.Axis0_TotalTime)
            Statistics.Axis1_Time = ControlRegister(Register.Axis1_TotalTime)
            Statistics.Axis2_Time = ControlRegister(Register.Axis2_TotalTime)
            Statistics.Axis3_Time = ControlRegister(Register.Axis3_TotalTime)
            Storage.saveRecord(Statistics)
        End If

        If DisplayOK Then
            If MotorState > 1 Then
                WriteNextion("va1.val=1")
            Else
                WriteNextion("va1.val=0")
            End If
            NextionUpdate(1)
        End If

        For j As Integer = 0 To 3
            If Get_Error(ODrive.Map_DRV(j), ODrive.Map_OUT(j)) <> 0 Then
                ODrive.Motor(j).Enable = False
                MotorEvent(ODrive.Map_DRV(j), ODrive.Map_OUT(j), ODrive.Motor(j).ErrText)
            End If
        Next

        If ODrive.Motor(0).Enable Then ODrive.Motor(0).Ready = True
        If ODrive.Motor(1).Enable Then ODrive.Motor(1).Ready = True
        If ODrive.Motor(2).Enable Then ODrive.Motor(2).Ready = True
        If ODrive.Motor(3).Enable Then ODrive.Motor(3).Ready = True

        If ScriptState = False Then Script_RUN = False

        If MotorState > 1 Then
            If Log.getStreamMode Then
                TelemetryEnable = True
            Else
                If FTP.Connected Then
                    If Not FTP.FileInProgress Then
                        Try
                            Debug.WriteLine("Create LOG")
                            'FTP.FileCreate(Log.GetDateTimeName & "_xDash_00_" & Log.getName & ".txt", True, False).Wait()
                            FTP.FileCreate(Log.getName(True), True, False).Wait()
                            Log.Clear()
                            Task.Delay(250).Wait()
                            FTP.FileSend(LOG_Version.ToString & " #LOG Version" & vbCrLf).Wait()
                            Debug.WriteLine("Start Telemetry")
                            TelemetryEnable = True
                        Catch ex As Exception
                            Debug.WriteLine("FTP Error")
                            Debug.WriteLine("Disable Telemetry")
                            TelemetryEnable = False
                        End Try
                    End If
                End If
            End If

        End If

        If MotorState = 1 Then
            If Log.getStreamMode Then
                TelemetryEnable = False
            Else
                If FTP.Connected Then
                    If FTP.FileInProgress Then
                        Debug.WriteLine("Stop Telemetry")
                        TelemetryEnable = False
                        Task.Delay(50).Wait()
                        Debug.WriteLine("Flush buffer")
                        Log.Flush()
                        Debug.WriteLine("Close LOG")
                        FTP.FileClose()
                    End If
                End If
            End If
        End If

        ControlRegister(Register.MotorControl_Busy) = 0

    End Sub

    Function GetNextionFloatNumber(FloatValue As Single, DecimalPoint As Integer) As Integer
        Return Convert.ToInt32(Math.Round(FloatValue, DecimalPoint) * (10 ^ DecimalPoint))
    End Function

    Sub WriteCommand(ByVal DeviceIndex As Integer, ByVal DeviceData As String)
        'Dim DeviceIndex As Integer
        'Dim DeviceData As String
        'Dim Distant As Integer = 0
        'Dim xLenght As Integer = xData.Length
        'Dim FilterString As String = xData.Replace(vbCr, " ")
        'FilterString = FilterString.Replace(vbLf, " ")
        'If xLenght > 2 Then
        '    Select Case xDev
        '        Case = 0
        '            Log.Data.Drive0TX = xData.Substring(0, xLenght - 2)
        '        Case = 1
        '            Log.Data.Drive1TX = xData.Substring(0, xLenght - 2)
        '        Case = 2
        '            Log.Data.BUSTX = xData
        '        Case = 3
        '            Log.Data.Display1TX = xData.Substring(0, xLenght - 3)
        '    End Select
        '    Log.Add()
        'End If
        'COMMBusy(xDev) = True

        SyncLock WriteLock
            'DeviceIndex = xDev
            'DeviceData = xData
            'Distant = RingOUTIndex(DeviceIndex)
            'If RingOUTIndex(DeviceIndex) < RingINIndex(DeviceIndex) Then
            '    Distant = RingOUTIndex(DeviceIndex) + 100
            'End If
            'If Math.Abs(RingINIndex(DeviceIndex) - Distant) < 50 Then
            RingWriteIndex(DeviceIndex) += 1
            If RingWriteIndex(DeviceIndex) >= RingBufferSize Then RingWriteIndex(DeviceIndex) = 0
            RingBuffer(DeviceIndex, RingWriteIndex(DeviceIndex)) = DeviceData
            'End If
            'If DeviceIndex < 2 Then Debug.WriteLine(DeviceIndex & "*" & DeviceData.Replace(vbLf, " "))
            'Debug.WriteLine(xData)
        End SyncLock

        ' If UseComm(xDev) Then CommWriter(xDev).WriteString(xData)
    End Sub

    Sub WriteRespondString(ByVal RespondData As String, ByVal resDes As Integer)
        'SyncLock RespondLock
        '    If RespondData <> "" Then
        '        If resDes > -1 And resDes < 2 Then
        '            RespondWriteIndex(resDes) += 1
        '            If RespondWriteIndex(resDes) >= RespondBufferSize Then RespondWriteIndex(resDes) = 0
        '            RespondBuffer(resDes, RespondWriteIndex(resDes)) = RespondData
        '        End If
        '    End If
        'End SyncLock
    End Sub

    Private Sub WriteRespondBinary(ByVal RespondByte() As Byte, ByVal resDes As Integer)
        SyncLock RespondLock
            Dim XLen As Integer = RespondByte.Length
            If resDes > -1 And resDes < 2 Then
                RespondWriteIndex(resDes) += 1
                If RespondWriteIndex(resDes) >= RespondBufferSize Then RespondWriteIndex(resDes) = 0

                'RespondWriteIndex(resDes) += 1
                'If RespondWriteIndex(resDes) >= RespondBufferSize Then RespondWriteIndex(resDes) = 0
                'RespondBuffer(resDes, RespondWriteIndex(resDes)) = RespondData

                RespondByteWritePointer = RespondWriteIndex(resDes) * (RespondPacketSize + RespondPacketSizeIndicator)
                If resDes = RespondTo.Dashboard Then RespondByteWritePointer += RespondByteMaxSize \ 2

                If XLen >= RespondPacketSize Then
                    System.Array.Copy(RespondByte, 0, RespondByteBuffer, RespondByteWritePointer, RespondPacketSize)
                    RespondByteBuffer(RespondByteWritePointer + RespondPacketSize) = 255
                    'Debug.Write(" ??" & RespondByteWritePointer.ToString & "-" & RespondByteBuffer(RespondByteWritePointer + RespondPacketSize))
                Else
                    For i As Integer = RespondByteWritePointer To XLen : RespondByteBuffer(i) = XLen : Next
                    System.Array.Copy(RespondByte, 0, RespondByteBuffer, RespondByteWritePointer, XLen)
                    RespondByteBuffer(RespondByteWritePointer + RespondPacketSize) = XLen
                    'Debug.Write(" ?" & RespondByteWritePointer.ToString & "-" & RespondByteBuffer(RespondByteWritePointer + RespondPacketSize))
                End If

            End If
        End SyncLock
    End Sub

    Sub WriteNextion(ByVal xData As String)
        Dim XASCII() As Byte
        Dim XOut() As Byte
        Dim XLen As Integer = 0
        'Dim ExtASCII As System.Text.Encoding = System.Text.Encoding.UTF8 'Windows-1252 single-byte character encoding (extended ASCII)
        If DisplayOK Then
            If xData <> "" Then
                ReDim XOut(xData.Length + 2)
                XASCII = System.Text.Encoding.ASCII.GetBytes(xData)
                System.Array.Copy(XASCII, XOut, XASCII.Length)
                XOut(XOut.Length - 3) = 255
                XOut(XOut.Length - 2) = 255
                XOut(XOut.Length - 1) = 255
            Else
                ReDim XOut(2)
                XOut(0) = 255
                XOut(1) = 255
                XOut(2) = 255
            End If
            SyncLock WriteByteLock
                RingWriteIndex(Reg.Device_Display) += 1
                If RingWriteIndex(Reg.Device_Display) >= RingBufferSize Then RingWriteIndex(Reg.Device_Display) = 0
                RingByteBuffer(Reg.Device_Display, RingWriteIndex(Reg.Device_Display)) = XOut
            End SyncLock
        End If
    End Sub

    Sub NextionUpdate(PageID As Integer)
        Dim PageNO As Integer = 0
        NextionUpdate_Inprogress = True
        PageNO = IORegister(3, Reg.IO_P)
        If PageID >= 0 Then PageNO = PageID
        If Not DisplayOK Then Exit Sub
        Select Case PageNO
            Case = 0
            Case = 1
                If Not UseComm(0) Then
                    WriteNextion("page_status.ms0.txt=""unknown""")
                    WriteNextion("page_status.ms1.txt=""unknown""")
                Else
                    WriteNextion("page_status.ms0.txt=""" & ODrive.Motor(0).Text & """")
                    WriteNextion("page_status.ms1.txt=""" & ODrive.Motor(1).Text & """")
                End If
                If Not UseComm(1) Then
                    WriteNextion("page_status.ms2.txt=""unknown""")
                    WriteNextion("page_status.ms3.txt=""unknown""")
                Else
                    WriteNextion("page_status.ms2.txt=""" & ODrive.Motor(2).Text & """")
                    WriteNextion("page_status.ms3.txt=""" & ODrive.Motor(3).Text & """")
                End If
                WriteNextion("page_status.ip0.txt=""" & LocalAddress.ToString & """")
                WriteNextion("page_status.ip1.txt=""" & LogAddress.ToString & """")
                WriteNextion("page_status.ip3.txt=""" & TelemetryAddress.ToString & """")
                WriteNextion("page_status.mt0.val=" & GetNextionFloatNumber(ControlRegister(Register.Axis0_TotalTime), 2).ToString)
                WriteNextion("page_status.mt1.val=" & GetNextionFloatNumber(ControlRegister(Register.Axis1_TotalTime), 2).ToString)
                WriteNextion("page_status.mt2.val=" & GetNextionFloatNumber(ControlRegister(Register.Axis2_TotalTime), 2).ToString)
                WriteNextion("page_status.mt3.val=" & GetNextionFloatNumber(ControlRegister(Register.Axis3_TotalTime), 2).ToString)
                WriteNextion("page_status.mr0.val=" & GetNextionFloatNumber(ControlRegister(Register.Axis0_TotalRev), 2).ToString)
                WriteNextion("page_status.mr1.val=" & GetNextionFloatNumber(ControlRegister(Register.Axis1_TotalRev), 2).ToString)
                WriteNextion("page_status.mr2.val=" & GetNextionFloatNumber(ControlRegister(Register.Axis2_TotalRev), 2).ToString)
                WriteNextion("page_status.mr3.val=" & GetNextionFloatNumber(ControlRegister(Register.Axis3_TotalRev), 2).ToString)
            Case = 2
            Case = 3
            Case = 4
                'WriteNextion("page_monitor.tl0.val=" & GetNextionFloatNumber(TelemetryDataS(Telemetry_Slot_Select, Telemetry_CUR_Measured), 2).ToString)

        End Select
        NextionUpdate_Inprogress = False
    End Sub

    Private Function OdriveCheck(DriveNo As Integer) As Boolean
        Dim resBool As Boolean = True
        Dim resString As String = ""
        Dim drvVoltage As Integer = 0
        Const versionSeparator As Char = ":"
        Const versionSpace As Char = " "
        Select Case DriveNo
            Case 0 : Log.Data.Drive0_ON = 1
            Case 1 : Log.Data.Drive1_ON = 1
            Case 2 : Log.Data.Drive2_ON = 1
            Case 3 : Log.Data.Drive3_ON = 1
        End Select
        For i As Integer = 0 To 2
            CommandProcessor(ODrive.ReadCommand(DriveNo, 0, ODrive.VBUS_VOLTAGE))
            Task.Delay(50).Wait()
        Next
        drvVoltage = Math.Truncate(IORegister(DriveNo, Reg.IO_HEAD))
        For i As Integer = 0 To 2
            WriteCommand(DriveNo, "i" & vbCrLf)
            Task.Delay(100).Wait()
        Next
        For Each xStr As String In CommRXShadow(DriveNo).Split(vbCrLf)
            If xStr.Contains(versionSeparator) Then
                resString = resString & xStr.Substring(xStr.LastIndexOf(versionSeparator))
                resString = resString.Replace(versionSpace, "")
            End If
        Next
        If resString.StartsWith(versionSeparator) Then resString = resString.Substring(1)
        Select Case DriveNo
            Case 0 : Log.Data.Supply0_Voltage = drvVoltage : Log.Data.Drive0_Info = resString
            Case 1 : Log.Data.Supply1_Voltage = drvVoltage : Log.Data.Drive1_Info = resString
            Case 2 : Log.Data.Supply2_Voltage = drvVoltage : Log.Data.Drive2_Info = resString
            Case 3 : Log.Data.Supply3_Voltage = drvVoltage : Log.Data.Drive3_Info = resString
        End Select
        If drvVoltage < 8 Then
            Select Case DriveNo
                Case 0 : Log.Data.Drive0_ON = ErrorValue_Int
                Case 1 : Log.Data.Drive1_ON = ErrorValue_Int
                Case 2 : Log.Data.Drive2_ON = ErrorValue_Int
                Case 3 : Log.Data.Drive3_ON = ErrorValue_Int
            End Select
            resBool = False
        End If
        Return resBool
    End Function

    Sub SysEvent(ByVal EventText As String)
        Log.Data.System_Event = EventText
        Debug.WriteLine(EventText)
    End Sub

    Function getBit(rawData As Single, bitPosition As Integer) As Integer
        Dim intVal As UInt32 = 0
        Dim bitMask = 0
        Dim res As Integer = 0
        If bitPosition >= 0 And bitPosition < 32 Then
            bitMask = 2 ^ bitPosition
            intVal = Math.Abs(Math.Truncate(rawData))
            If (intVal And bitMask) > 0 Then res = 1
            'Debug.WriteLine("MASKED : " & (intVal And bitMask).ToString)
        End If
        Return res
    End Function
    ' -------- This part add for USB game controller support --------

    Public ReadOnly Property Headset As Headset Implements IGameController.Headset
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public ReadOnly Property IsWireless As Boolean Implements IGameController.IsWireless
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public ReadOnly Property User As User Implements IGameController.User
        Get
            Throw New NotImplementedException()
        End Get
    End Property

    Public Event HeadsetConnected As TypedEventHandler(Of IGameController, Headset) Implements IGameController.HeadsetConnected
    Public Event HeadsetDisconnected As TypedEventHandler(Of IGameController, Headset) Implements IGameController.HeadsetDisconnected
    Public Event UserChanged As TypedEventHandler(Of IGameController, UserChangedEventArgs) Implements IGameController.UserChanged

    Public Function TryGetBatteryReport() As BatteryReport Implements IGameControllerBatteryInfo.TryGetBatteryReport
        Throw New NotImplementedException()
    End Function

End Class

