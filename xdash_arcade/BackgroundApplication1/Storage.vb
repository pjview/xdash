Module Storage

    Structure Machine_Statistics
        Dim Axis0_Time As Single
        Dim Axis1_Time As Single
        Dim Axis2_Time As Single
        Dim Axis3_Time As Single
        Dim Axis0_Revolution As Single
        Dim Axis1_Revolution As Single
        Dim Axis2_Revolution As Single
        Dim Axis3_Revolution As Single
        Dim Coin_Total As Integer
        Dim ServiceBTN_Total As Integer
    End Structure

    Structure Motor_Profile
        Dim Current_Limit As Single
        Dim Current_Limit_Tolerance As Single
        Dim Current_Range As Single
        Dim Velocity_Limit As Single
        Dim Trajectory_Velocity_Limit As Single
        Dim Acceleration_Limit As Single
        Dim Deacceleration_Limit As Single
        Dim Position_Gain As Single
        'ODrive.CommonPreset(0).Velocity_Gain = 0.0011
        'ODrive.CommonPreset(0).Velocity_Integrator_Gain = 0.0002
    End Structure

    Structure Hardware_Config
        Dim Drive0_DeviceName As String
        Dim Drive1_DeviceName As String
        Dim Drive2_DeviceName As String
        Dim Drive3_DeviceName As String
        Dim Drive0_BPS As Integer
        Dim Drive1_BPS As Integer
        Dim Drive2_BPS As Integer
        Dim Drive3_BPS As Integer
        Dim System_Name As String
        Dim Display_DeviceName As String
        Dim Control_DeviceName As String
        Dim Local_DeviceName As String
        Dim Display_BPS As Integer
        Dim Control_BPS As Integer
        Dim Local_BPS As Integer
        Dim Timeout_USBRead As Integer
        Dim Timeout_USBWrite As Integer
        Dim Motor0_DriveNo As Integer
        Dim Motor0_OutputNo As Integer
        Dim Motor0_SensorNo As Integer
        Dim Motor0_SensorMode As Integer
        Dim Motor1_DriveNo As Integer
        Dim Motor1_OutputNo As Integer
        Dim Motor1_SensorNo As Integer
        Dim Motor1_SensorMode As Integer
        Dim Motor2_DriveNo As Integer
        Dim Motor2_OutputNo As Integer
        Dim Motor2_SensorNo As Integer
        Dim Motor2_SensorMode As Integer
        Dim Motor3_DriveNo As Integer
        Dim Motor3_OutputNo As Integer
        Dim Motor3_SensorNo As Integer
        Dim Motor3_SensorMode As Integer
        Dim Motor4_DriveNo As Integer
        Dim Motor4_OutputNo As Integer
        Dim Motor4_SensorNo As Integer
        Dim Motor4_SensorMode As Integer
        Dim Motor5_DriveNo As Integer
        Dim Motor5_OutputNo As Integer
        Dim Motor5_SensorNo As Integer
        Dim Motor5_SensorMode As Integer
        Dim Motor6_DriveNo As Integer
        Dim Motor6_OutputNo As Integer
        Dim Motor6_SensorNo As Integer
        Dim Motor6_SensorMode As Integer
        Dim Motor7_DriveNo As Integer
        Dim Motor7_OutputNo As Integer
        Dim Motor7_SensorNo As Integer
        Dim Motor7_SensorMode As Integer
    End Structure

    Structure Network_Config
        Dim Control_IP As System.Net.IPAddress
        Dim Control_PortRX As UInt16
        Dim Control_PortTXSource As UInt16
        Dim Control_PortTXTarget As UInt16
        Dim FTPServer_IP As System.Net.IPAddress
        Dim Dashboard_IP As System.Net.IPAddress
        Dim Dashboard_PortTXSource As UInt16
        Dim DashboardStatus_PortTXTarget As UInt16
        Dim DashboardCommand_PortTXTarget As UInt16
        Dim Telemetry_IP As System.Net.IPAddress
        Dim Telemetry_PortTXSource As UInt16
        Dim Telemetry_PortTXTarget As UInt16
        Dim ConfigOK As Boolean
    End Structure

    Structure DOF_Config
        Dim DOF_Type() As UInt16
        Dim DOF_Axis0DIR() As Boolean
        Dim DOF_Axis0Percentage() As Double
        Dim DOF_Axis1DIR() As Boolean
        Dim DOF_Axis1Percentage() As Double
        Dim DOF_Axis2DIR() As Boolean
        Dim DOF_Axis2Percentage() As Double
        Dim DOF_Axis3DIR() As Boolean
        Dim DOF_Axis3Percentage() As Double
        Dim DOF_Axis4DIR() As Boolean
        Dim DOF_Axis4Percentage() As Double
        Dim DOF_Axis5DIR() As Boolean
        Dim DOF_Axis5Percentage() As Double
    End Structure

    Structure OP_Config
        Dim Delay_ParameterSet As Integer
        Dim Delay_StateChange As Integer
        Dim Auto_Start As Integer

        Dim Telemetry_Timeout As Integer
        Dim Telemetry_Mode As Integer
        Dim Telemetry_Interval As Integer
        Dim Axis_ConfigOK() As Boolean
        Dim Axis_HomeEnable() As Boolean
        Dim Axis_IndexEnable() As Boolean
        Dim Axis_EndstopEnable() As Boolean
        Dim Axis_EndstopOffset() As Integer
        Dim Axis_HomeDeg() As Single
        Dim Axis_HomeDir() As Boolean
        Dim Axis_HomeEnc() As Integer
        Dim Axis_HomeLimit() As Integer
        Dim Axis_HomeTotalance() As Single
        Dim Axis_HomeTimeout() As Integer
        Dim Axis_IndexTimeout() As Integer
        Dim Axis_Type() As Integer
        Dim Axis_PPR() As Integer
        Dim Axis_RangePositive() As Single
        Dim Axis_RangeNegative() As Single
        Dim Axis_GearRatio() As Single
        Dim Axis_CalibrationProfile() As Integer
        Dim Axis_RunProfile() As Integer
        Dim Axis_ParkProfile() As Integer
        Dim Profile_ConfigOK() As Boolean
        Dim Profile_CurrentLimit() As Single
        Dim Profile_CurrentLimitTolerance() As Single
        Dim Profile_CurrentRange() As Single
        Dim Profile_CurrentControlBandwidth() As Single
        Dim Profile_VelocityLimit() As Single
        Dim Profile_TrajectoryVelocityLimit() As Single
        Dim Profile_AccelerationLimit() As Single
        Dim Profile_DeaccelerationLimit() As Single
        Dim Profile_PositionGain() As Single
        Dim Profile_VelocityGain() As Single
        Dim Profile_VelocityIntegratorGain() As Single
        Dim Profile_CalibrationCurrent() As Single
        Dim Profile_CalibrationVelocity() As Single
        Dim Profile_CalibrationAcceleration() As Single
        Dim Profile_CalibrationRamp() As Single

    End Structure

    Structure MS_Config
        Dim MotorType As Integer
        Dim MotorPolePaire As Integer
        Dim MotorPreCalibrate As Integer
        Dim EncoderPreCalibrate As Integer
        Dim EncoderCPR As Integer
        Dim EncoderUseIndex As Integer
        Dim CalibrationVoltage As Single
    End Structure

    Public Enum TelemetryMode As Integer
        Disable = 0
        LocalStorage = 1
        FTP = 2
        LiveStream = 3
    End Enum

    Private Declare Function GetDiskFreeSpaceEx Lib "kernel32" Alias "GetDiskFreeSpaceExA" (ByVal lpDirectoryName As String, ByRef lpFreeBytesAvailableToMe As Long, ByRef lpTotalNumberOfBytes As Long, ByRef lpTotalNumberOfFreeBytes As Long) As Integer

    Private Const LogFolder As String = "Log"
    Private Const ConfigFolder As String = "Config"
    Private Const RecordFileName As String = "record.txt"
    Private Const HistoryFilename As String = "history.txt"
    Private Const NetworkConfigFilename As String = "net_conf.txt"
    Private Const HardwareConfigFilename As String = "hw_conf.txt"
    Private Const DOFConfigFilename As String = "dof_conf.txt"
    Private Const OperationConfigFilename As String = "op_conf.txt"
    Private Const MotorConfigfilename As String = "ms_conf.txt"

    Private Const ErrorValue_Int As Integer = -32768
    Private Const ErrorValue_Single As Single = -32768.0

    Private myStorage As System.IO.IsolatedStorage.IsolatedStorageFile = System.IO.IsolatedStorage.IsolatedStorageFile.GetUserStoreForApplication

    Public Function getSpace(ByVal driveStr As String) As Integer
        Dim x As New System.IO.DirectoryInfo("C:\")
        Dim res As Integer = 0
        Dim SpaceAvailable As Integer = 0
        Dim TotalByte As Integer = 0
        Dim FreeByte As Integer = 0
        Dim xRes As Integer = 0
        'Dim y As System.IO.FileSystemInfo = x.GetFileSystemInfos()
        Dim LocalDrive As String = System.AppContext.BaseDirectory
        xRes = GetDiskFreeSpaceEx(LocalDrive, SpaceAvailable, TotalByte, FreeByte)
        If xRes > 0 Then res = SpaceAvailable
        Return res
    End Function

    Public Function loadRecord() As Machine_Statistics
        Dim Statistics As Machine_Statistics
        Dim RData As String = ""
        If Not myStorage.DirectoryExists(LogFolder) Then
            myStorage.CreateDirectory(LogFolder)
            Debug.WriteLine("create log folder")
        End If
        If Not myStorage.FileExists(LogFolder & "\\" & RecordFileName) Then
            Debug.WriteLine("create record file")
            Dim newReg As New System.IO.StreamWriter(New System.IO.IsolatedStorage.IsolatedStorageFileStream(LogFolder & "\\" & RecordFileName, FileMode.Create, FileAccess.Write, myStorage))
            newReg.WriteLine("TotalRev_Drv0_M0=0")
            newReg.WriteLine("TotalRev_Drv0_M1=0")
            newReg.WriteLine("TotalRev_Drv1_M0=0")
            newReg.WriteLine("TotalRev_Drv1_M1=0")
            newReg.WriteLine("TotalTime_Drv0_M0=0")
            newReg.WriteLine("TotalTime_Drv0_M1=0")
            newReg.WriteLine("TotalTime_Drv1_M0=0")
            newReg.WriteLine("TotalTime_Drv1_M1=0")
            newReg.WriteLine("TotalCoin=0")
            newReg.WriteLine("TotalService=0")
            newReg.Flush()
            newReg.Dispose()
            Statistics.Axis0_Revolution = 0
            Statistics.Axis1_Revolution = 0
            Statistics.Axis2_Revolution = 0
            Statistics.Axis3_Revolution = 0
            Statistics.Axis0_Time = 0
            Statistics.Axis1_Time = 0
            Statistics.Axis2_Time = 0
            Statistics.Axis3_Time = 0
        Else
            Debug.WriteLine("read record file...")
            Dim existingLog As New System.IO.StreamReader(New System.IO.IsolatedStorage.IsolatedStorageFileStream(LogFolder & "\\" & RecordFileName, FileMode.Open, FileAccess.Read, myStorage))
            Do Until (existingLog.EndOfStream)
                RData = existingLog.ReadLine()
                If RData.StartsWith("TotalRev_Drv0_M0") Then Statistics.Axis0_Revolution = Single.Parse(RData.Substring(RData.IndexOf("=") + 1).Trim)
                If RData.StartsWith("TotalRev_Drv0_M1") Then Statistics.Axis1_Revolution = Single.Parse(RData.Substring(RData.IndexOf("=") + 1).Trim)
                If RData.StartsWith("TotalRev_Drv1_M0") Then Statistics.Axis2_Revolution = Single.Parse(RData.Substring(RData.IndexOf("=") + 1).Trim)
                If RData.StartsWith("TotalRev_Drv1_M1") Then Statistics.Axis3_Revolution = Single.Parse(RData.Substring(RData.IndexOf("=") + 1).Trim)
                If RData.StartsWith("TotalTime_Drv0_M0") Then Statistics.Axis0_Time = Single.Parse(RData.Substring(RData.IndexOf("=") + 1).Trim)
                If RData.StartsWith("TotalTime_Drv0_M1") Then Statistics.Axis1_Time = Single.Parse(RData.Substring(RData.IndexOf("=") + 1).Trim)
                If RData.StartsWith("TotalTime_Drv1_M0") Then Statistics.Axis2_Time = Single.Parse(RData.Substring(RData.IndexOf("=") + 1).Trim)
                If RData.StartsWith("TotalTime_Drv1_M1") Then Statistics.Axis3_Time = Single.Parse(RData.Substring(RData.IndexOf("=") + 1).Trim)
                If RData.StartsWith("TotalCoin") Then Statistics.Coin_Total = Integer.Parse(RData.Substring(RData.IndexOf("=") + 1).Trim)
                If RData.StartsWith("TotalService") Then Statistics.ServiceBTN_Total = Integer.Parse(RData.Substring(RData.IndexOf("=") + 1).Trim)
            Loop
            existingLog.Dispose()
            Debug.WriteLine("finish")
        End If
        Return Statistics
    End Function

    Public Sub saveRecord(Statistics As Machine_Statistics)
        Debug.WriteLine("Update record file...")
        Dim newLog As New System.IO.StreamWriter(New System.IO.IsolatedStorage.IsolatedStorageFileStream(LogFolder & "\\" & RecordFileName, FileMode.Open, FileAccess.Write, myStorage))
        newLog.WriteLine("TotalRev_Drv0_M0=" & Statistics.Axis0_Revolution.ToString)
        newLog.WriteLine("TotalRev_Drv0_M1=" & Statistics.Axis1_Revolution.ToString)
        newLog.WriteLine("TotalRev_Drv1_M0=" & Statistics.Axis2_Revolution.ToString)
        newLog.WriteLine("TotalRev_Drv1_M1=" & Statistics.Axis3_Revolution.ToString)
        newLog.WriteLine("TotalTime_Drv0_M0=" & Statistics.Axis0_Time.ToString)
        newLog.WriteLine("TotalTime_Drv0_M1=" & Statistics.Axis1_Time.ToString)
        newLog.WriteLine("TotalTime_Drv1_M0=" & Statistics.Axis2_Time.ToString)
        newLog.WriteLine("TotalTime_Drv1_M1=" & Statistics.Axis3_Time.ToString)
        newLog.Flush()
        newLog.Dispose()
        Debug.WriteLine("finish")
    End Sub

    Public Sub updateHistory()

        Debug.WriteLine("write history file...")
        Dim FWriter As New System.IO.StreamReader(New System.IO.IsolatedStorage.IsolatedStorageFileStream(LogFolder & "\\" & HistoryFilename, FileMode.Append, FileAccess.Write, myStorage))

        FWriter.Dispose()
        Debug.WriteLine("finish")

    End Sub

    Public Function LoadNetConfig() As Network_Config
        Const Default_IP As String = "127.0.0.1"
        Const Default_CtrlRX As UInt16 = 10000
        Const Default_CtrlTXSource As UInt16 = 10020
        Const Default_CtrlTXTarget As UInt16 = 10010
        Const Default_DashTXSource As UInt16 = 10021
        Const Default_DashStatusTXTarget As UInt16 = 8102
        Const Default_DashCommandTXTarget As UInt16 = 8100
        Const Default_TeleTXSource As UInt16 = 10022
        Const Default_TeleTXTarget As UInt16 = 8110
        Dim xConfig As Network_Config
        Dim RData As String = ""
        Dim ConfigTest(50) As Integer
        Dim ConfigCheck As Integer = 0
        Dim strLocalIP As String = ""
        Dim strHostControlIP As String = ""
        CheckConfigFolder()
        xConfig.Control_IP = System.Net.IPAddress.Parse(Default_IP)
        xConfig.Control_PortRX = Default_CtrlRX
        xConfig.Control_PortTXSource = Default_CtrlTXSource
        xConfig.Control_PortTXTarget = Default_CtrlTXTarget
        xConfig.Telemetry_IP = System.Net.IPAddress.Parse(Default_IP)
        xConfig.FTPServer_IP = System.Net.IPAddress.Parse(Default_IP)
        xConfig.Dashboard_IP = System.Net.IPAddress.Parse(Default_IP)
        xConfig.Dashboard_PortTXSource = Default_DashTXSource
        xConfig.DashboardStatus_PortTXTarget = Default_DashStatusTXTarget
        xConfig.DashboardCommand_PortTXTarget = Default_DashCommandTXTarget
        xConfig.Telemetry_PortTXSource = Default_TeleTXSource
        xConfig.Telemetry_PortTXTarget = Default_TeleTXTarget

        If Not myStorage.FileExists(ConfigFolder & "\\" & NetworkConfigFilename) Then
            Debug.WriteLine("create network config file")
            Dim newReg As New System.IO.StreamWriter(New System.IO.IsolatedStorage.IsolatedStorageFileStream(ConfigFolder & "\\" & NetworkConfigFilename, FileMode.Create, FileAccess.Write, myStorage))
            'newReg.WriteLine("#Telemetry Mode (0:Disable 1:Local-Storage 2:FTP 3:LiveStream)")
            newReg.WriteLine("HostControl_IP=" & Default_IP)
            newReg.WriteLine("Control_IP=" & Default_IP)
            newReg.WriteLine("ControlRX_Port=" & Default_CtrlRX.ToString)
            newReg.WriteLine("ControlTXSource_Port=" & Default_CtrlTXSource.ToString)
            newReg.WriteLine("ControlTXTarget_Port=" & Default_CtrlTXTarget.ToString)
            newReg.WriteLine("FTPServer_IP=" & Default_IP)
            newReg.WriteLine("Dashboard_IP=" & Default_IP)
            newReg.WriteLine("DashboardTXSource_Port=" & Default_DashTXSource.ToString)
            newReg.WriteLine("DashboardStaTXTarget_Port=" & Default_DashStatusTXTarget.ToString)
            newReg.WriteLine("DashboardCmdTXTarget_Port=" & Default_DashCommandTXTarget.ToString)
            newReg.WriteLine("TelemetryRX_IP=" & Default_IP)
            newReg.WriteLine("TelemetryTXSource_Port=" & Default_TeleTXSource.ToString)
            newReg.WriteLine("TelemetryTXTarget_Port=" & Default_TeleTXTarget.ToString)
            newReg.Flush()
            newReg.Dispose()
        Else
            Debug.WriteLine("read network config file...")
            Dim existingFile As New System.IO.StreamReader(New System.IO.IsolatedStorage.IsolatedStorageFileStream(ConfigFolder & "\\" & NetworkConfigFilename, FileMode.Open, FileAccess.Read, myStorage))
            Do Until (existingFile.EndOfStream)
                RData = existingFile.ReadLine()
                If RData.StartsWith("HostControl_IP") Then System.Net.IPAddress.TryParse(getStrValue(RData), xConfig.Control_IP) : ConfigTest(0) += 1
                'If RData.StartsWith("Control_IP") Then System.Net.IPAddress.TryParse(getStrValue(RData), xConfig.Control_IP) : ConfigTest(0) += 1
                If RData.StartsWith("ControlRX_Port") Then UInt16.TryParse(getStrValue(RData), xConfig.Control_PortRX) : ConfigTest(1) += 1
                If RData.StartsWith("ControlTXSource_Port") Then UInt16.TryParse(getStrValue(RData), xConfig.Control_PortTXSource) : ConfigTest(2) += 1
                If RData.StartsWith("ControlTXTarget_Port") Then UInt16.TryParse(getStrValue(RData), xConfig.Control_PortTXTarget) : ConfigTest(3) += 1
                If RData.StartsWith("FTPServer_IP") Then System.Net.IPAddress.TryParse(getStrValue(RData), xConfig.FTPServer_IP) : ConfigTest(4) += 1
                If RData.StartsWith("Dashboard_IP") Then System.Net.IPAddress.TryParse(getStrValue(RData), xConfig.Dashboard_IP) : ConfigTest(5) += 1
                If RData.StartsWith("DashboardTXSource_Port") Then UInt16.TryParse(getStrValue(RData), xConfig.Dashboard_PortTXSource) : ConfigTest(6) += 1
                If RData.StartsWith("DashboardStaTXTarget_Port") Then UInt16.TryParse(getStrValue(RData), xConfig.DashboardStatus_PortTXTarget) : ConfigTest(7) += 1
                If RData.StartsWith("DashboardCmdTXTarget_Port") Then UInt16.TryParse(getStrValue(RData), xConfig.DashboardCommand_PortTXTarget) : ConfigTest(8) += 1
                If RData.StartsWith("TelemetryRX_IP") Then System.Net.IPAddress.TryParse(getStrValue(RData), xConfig.Telemetry_IP) : ConfigTest(9) += 1
                If RData.StartsWith("TelemetryTXSource_Port") Then UInt16.TryParse(getStrValue(RData), xConfig.Telemetry_PortTXSource) : ConfigTest(10) += 1
                If RData.StartsWith("TelemetryTXTarget_Port") Then UInt16.TryParse(getStrValue(RData), xConfig.Telemetry_PortTXTarget) : ConfigTest(11) += 1
            Loop
            existingFile.Dispose()
            Debug.WriteLine("finish")
        End If

        xConfig.ConfigOK = True
        For i As Integer = 0 To ConfigTest.Length - 1
            ConfigCheck += ConfigTest(i)
            If ConfigTest(i) > 1 Then xConfig.ConfigOK = False
        Next
        If ConfigCheck <> 12 Then xConfig.ConfigOK = False

        Return xConfig
    End Function

    Public Function LoadHWConfig() As Hardware_Config
        Dim ConfigName(100) As String
        Dim xConfig As Hardware_Config
        Dim RData As String = ""
        Dim VData As String = ""
        Dim CIDX As Integer = 0
        Dim ConfigID As Integer = 0
        Dim RandomSource As New System.Random
        Dim RandomID As String = ""
        xConfig.Drive0_DeviceName = ""
        xConfig.Drive1_DeviceName = ""
        xConfig.Drive2_DeviceName = ""
        xConfig.Drive3_DeviceName = ""
        xConfig.Display_DeviceName = ""
        xConfig.Control_DeviceName = ""
        xConfig.Local_DeviceName = ""
        xConfig.System_Name = "NONAME"
        xConfig.Motor0_DriveNo = -1
        xConfig.Motor1_DriveNo = -1
        xConfig.Motor2_DriveNo = -1
        xConfig.Motor3_DriveNo = -1
        xConfig.Motor4_DriveNo = -1
        xConfig.Motor5_DriveNo = -1
        xConfig.Motor6_DriveNo = -1
        xConfig.Motor7_DriveNo = -1
        xConfig.Motor0_SensorNo = -1
        xConfig.Motor1_SensorNo = -1
        xConfig.Motor2_SensorNo = -1
        xConfig.Motor3_SensorNo = -1
        xConfig.Motor4_SensorNo = -1
        xConfig.Motor6_SensorNo = -1
        xConfig.Motor7_SensorNo = -1
        xConfig.Motor0_SensorMode = -1
        xConfig.Motor1_SensorMode = -1
        xConfig.Motor2_SensorMode = -1
        xConfig.Motor3_SensorMode = -1
        xConfig.Motor4_SensorMode = -1
        xConfig.Motor5_SensorMode = -1
        xConfig.Motor6_SensorMode = -1
        xConfig.Motor7_SensorMode = -1
        xConfig.Display_BPS = 0
        xConfig.Control_BPS = 0
        xConfig.Local_BPS = 0
        xConfig.Timeout_USBRead = 5
        xConfig.Timeout_USBWrite = 1000
        RandomID = String.Format("{0:D4}", RandomSource.Next(0, 9999))
        System.Array.Clear(ConfigName, 0, ConfigName.Length)

        For i As Integer = 1 To 4
            ConfigName(CIDX) = "Drive" & (i - 1).ToString & "_DeviceName" : CIDX += 1
            ConfigName(CIDX) = "Drive" & (i - 1).ToString & "_BPS" : CIDX += 1
        Next
        ConfigName(CIDX) = "Display_DeviceName" : CIDX += 1 'ConfigID : 8
        ConfigName(CIDX) = "Display_BPS" : CIDX += 1        'ConfigID : 9
        ConfigName(CIDX) = "Control_DeviceName" : CIDX += 1 'ConfigID : 10
        ConfigName(CIDX) = "Control_BPS" : CIDX += 1        'ConfigID : 11
        ConfigName(CIDX) = "Local_DeviceName" : CIDX += 1   'ConfigID : 12
        ConfigName(CIDX) = "Local_BPS" : CIDX += 1          'ConfigID : 13
        ConfigName(CIDX) = "Timeout_USBRead" : CIDX += 1    'ConfigID : 14
        ConfigName(CIDX) = "Timeout_Write" : CIDX += 1      'ConfigID : 15
        ConfigName(CIDX) = "System_Name" : CIDX += 1        'ConfigID : 16
        For i As Integer = 1 To 8
            ConfigName(CIDX) = "Motor" & (i - 1).ToString & "_DriveNo" : CIDX += 1
            ConfigName(CIDX) = "Motor" & (i - 1).ToString & "_OutputNo" : CIDX += 1
            ConfigName(CIDX) = "Motor" & (i - 1).ToString & "_SensorNo" : CIDX += 1
            ConfigName(CIDX) = "Motor" & (i - 1).ToString & "_SensorMode" : CIDX += 1
        Next

        CheckConfigFolder()
        If Not myStorage.FileExists(ConfigFolder & "\\" & HardwareConfigFilename) Then
            Debug.WriteLine("create hardware config file")
            Dim newReg As New System.IO.StreamWriter(New System.IO.IsolatedStorage.IsolatedStorageFileStream(ConfigFolder & "\\" & HardwareConfigFilename, FileMode.Create, FileAccess.Write, myStorage))
            For i As Integer = 0 To ConfigName.Length - 1
                If ConfigName(i) = "" Then Exit For
                If i = 16 Then
                    newReg.WriteLine(ConfigName(i) & "=NONAME_" & RandomID)
                Else
                    newReg.WriteLine(ConfigName(i) & "= ")
                End If

            Next
            newReg.Flush()
            newReg.Dispose()
        Else
            Debug.WriteLine("read hardware config file...")
            Dim existingFile As New System.IO.StreamReader(New System.IO.IsolatedStorage.IsolatedStorageFileStream(ConfigFolder & "\\" & HardwareConfigFilename, FileMode.Open, FileAccess.Read, myStorage))
            Do Until (existingFile.EndOfStream)
                RData = existingFile.ReadLine()
                ConfigID = -1
                For i As Integer = 0 To ConfigName.Length - 1
                    If ConfigName(i) = "" Then Exit For
                    If RData.StartsWith(ConfigName(i), StringComparison.Ordinal) Then
                        ConfigID = i
                        VData = getStrValue(RData).Trim
                        Exit For
                    End If
                Next
                Select Case ConfigID
                    Case 0 : xConfig.Drive0_DeviceName = VData.Trim
                    Case 1 : xConfig.Drive0_BPS = getIntValue(VData)
                    Case 2 : xConfig.Drive1_DeviceName = VData.Trim
                    Case 3 : xConfig.Drive1_BPS = getIntValue(VData)
                    Case 4 : xConfig.Drive2_DeviceName = VData.Trim
                    Case 5 : xConfig.Drive2_BPS = getIntValue(VData)
                    Case 6 : xConfig.Drive3_DeviceName = VData.Trim
                    Case 7 : xConfig.Drive3_BPS = getIntValue(VData)
                    Case 8 : xConfig.Display_DeviceName = VData.Trim
                    Case 9 : xConfig.Display_BPS = getIntValue(VData)
                    Case 10 : xConfig.Control_DeviceName = VData.Trim
                    Case 11 : xConfig.Control_BPS = getIntValue(VData)
                    Case 12 : xConfig.Local_DeviceName = VData.Trim
                    Case 13 : xConfig.Local_BPS = getIntValue(VData)
                    Case 14 : xConfig.Timeout_USBRead = getIntValue(VData)
                    Case 15 : xConfig.Timeout_USBWrite = getIntValue(VData)
                    Case 16 : xConfig.System_Name = VData.Trim

                    Case 17 : xConfig.Motor0_DriveNo = getIntValue(VData)
                    Case 18 : xConfig.Motor0_OutputNo = getIntValue(VData)
                    Case 19 : xConfig.Motor0_SensorNo = getIntValue(VData)
                    Case 20 : xConfig.Motor0_SensorMode = getIntValue(VData)

                    Case 21 : xConfig.Motor1_DriveNo = getIntValue(VData)
                    Case 22 : xConfig.Motor1_OutputNo = getIntValue(VData)
                    Case 23 : xConfig.Motor1_SensorNo = getIntValue(VData)
                    Case 24 : xConfig.Motor1_SensorMode = getIntValue(VData)

                    Case 25 : xConfig.Motor2_DriveNo = getIntValue(VData)
                    Case 26 : xConfig.Motor2_OutputNo = getIntValue(VData)
                    Case 27 : xConfig.Motor2_SensorNo = getIntValue(VData)
                    Case 28 : xConfig.Motor2_SensorMode = getIntValue(VData)

                    Case 29 : xConfig.Motor3_DriveNo = getIntValue(VData)
                    Case 30 : xConfig.Motor3_OutputNo = getIntValue(VData)
                    Case 31 : xConfig.Motor3_SensorNo = getIntValue(VData)
                    Case 32 : xConfig.Motor3_SensorMode = getIntValue(VData)

                    Case 33 : xConfig.Motor4_DriveNo = getIntValue(VData)
                    Case 34 : xConfig.Motor4_OutputNo = getIntValue(VData)
                    Case 35 : xConfig.Motor4_SensorNo = getIntValue(VData)
                    Case 36 : xConfig.Motor4_SensorMode = getIntValue(VData)

                    Case 37 : xConfig.Motor5_DriveNo = getIntValue(VData)
                    Case 38 : xConfig.Motor5_OutputNo = getIntValue(VData)
                    Case 39 : xConfig.Motor5_SensorNo = getIntValue(VData)
                    Case 40 : xConfig.Motor5_SensorMode = getIntValue(VData)

                    Case 41 : xConfig.Motor6_DriveNo = getIntValue(VData)
                    Case 42 : xConfig.Motor6_OutputNo = getIntValue(VData)
                    Case 43 : xConfig.Motor6_SensorNo = getIntValue(VData)
                    Case 44 : xConfig.Motor6_SensorMode = getIntValue(VData)

                    Case 45 : xConfig.Motor7_DriveNo = getIntValue(VData)
                    Case 46 : xConfig.Motor7_OutputNo = getIntValue(VData)
                    Case 47 : xConfig.Motor7_SensorNo = getIntValue(VData)
                    Case 48 : xConfig.Motor7_SensorMode = getIntValue(VData)
                End Select
            Loop
            existingFile.Dispose()

            If xConfig.Motor0_OutputNo < 0 Then xConfig.Motor0_DriveNo = -1
            If xConfig.Motor1_OutputNo < 0 Then xConfig.Motor1_DriveNo = -1
            If xConfig.Motor2_OutputNo < 0 Then xConfig.Motor2_DriveNo = -1
            If xConfig.Motor3_OutputNo < 0 Then xConfig.Motor3_DriveNo = -1
            If xConfig.Motor4_OutputNo < 0 Then xConfig.Motor4_DriveNo = -1
            If xConfig.Motor5_OutputNo < 0 Then xConfig.Motor5_DriveNo = -1
            If xConfig.Motor6_OutputNo < 0 Then xConfig.Motor6_DriveNo = -1
            If xConfig.Motor7_OutputNo < 0 Then xConfig.Motor7_DriveNo = -1

            If xConfig.Drive0_BPS < 9600 Then xConfig.Drive0_BPS = -1
            If xConfig.Drive1_BPS < 9600 Then xConfig.Drive1_BPS = -1
            If xConfig.Drive2_BPS < 9600 Then xConfig.Drive2_BPS = -1
            If xConfig.Drive3_BPS < 9600 Then xConfig.Drive3_BPS = -1
            If xConfig.Display_BPS < 9600 Then xConfig.Display_BPS = -1
            If xConfig.Control_BPS < 9600 Then xConfig.Control_BPS = -1
            If xConfig.Local_BPS < 9600 Then xConfig.Local_BPS = -1

            If xConfig.Drive0_DeviceName.Trim.ToUpper = "NULL" Then xConfig.Drive0_DeviceName = ""
            If xConfig.Drive1_DeviceName.Trim.ToUpper = "NULL" Then xConfig.Drive1_DeviceName = ""
            If xConfig.Drive2_DeviceName.Trim.ToUpper = "NULL" Then xConfig.Drive2_DeviceName = ""
            If xConfig.Drive3_DeviceName.Trim.ToUpper = "NULL" Then xConfig.Drive3_DeviceName = ""

            Debug.WriteLine("finish")
        End If
        Return xConfig
    End Function

    Public Function LoadDOFConfig() As DOF_Config
        Dim ConfigName(100) As String
        Dim VData As String = ""
        Dim CIDX As Integer = 0
        Dim ConfigID As Integer = -1
        Dim DOFID As Integer = -1
        Dim DOFName() As String = {"roll", "pitch", "surge", "sway", "heave", "yaw", "none", "none", "none", "none"}
        Dim CNIDX As Integer = 0
        Dim CTIDX(100) As Boolean
        Dim CPIDX(100) As Boolean
        Dim CDIDX(100) As Boolean
        Dim xConfig As DOF_Config
        ReDim xConfig.DOF_Type(6)
        ReDim xConfig.DOF_Axis0DIR(6)
        ReDim xConfig.DOF_Axis0Percentage(6)
        ReDim xConfig.DOF_Axis1DIR(6)
        ReDim xConfig.DOF_Axis1Percentage(6)
        ReDim xConfig.DOF_Axis2DIR(6)
        ReDim xConfig.DOF_Axis2Percentage(6)
        ReDim xConfig.DOF_Axis3DIR(6)
        ReDim xConfig.DOF_Axis3Percentage(6)
        ReDim xConfig.DOF_Axis4DIR(6)
        ReDim xConfig.DOF_Axis4Percentage(6)
        ReDim xConfig.DOF_Axis5DIR(6)
        ReDim xConfig.DOF_Axis5Percentage(6)

        Dim RData As String = ""

        For i As Integer = 0 To 78 Step 13
            ConfigName(i) = "DOF" & CIDX.ToString & "_Type" : CTIDX(i) = True
            ConfigName(i + 1) = "DOF" & CIDX.ToString & "_Axis0Percentage" : CPIDX(i + 1) = True
            ConfigName(i + 2) = "DOF" & CIDX.ToString & "_Axis0DIR" : CDIDX(i + 2) = True
            ConfigName(i + 3) = "DOF" & CIDX.ToString & "_Axis1Percentage" : CPIDX(i + 3) = True
            ConfigName(i + 4) = "DOF" & CIDX.ToString & "_Axis1DIR" : CDIDX(i + 4) = True
            ConfigName(i + 5) = "DOF" & CIDX.ToString & "_Axis2Percentage" : CPIDX(i + 5) = True
            ConfigName(i + 6) = "DOF" & CIDX.ToString & "_Axis2DIR" : CDIDX(i + 6) = True
            ConfigName(i + 7) = "DOF" & CIDX.ToString & "_Axis3Percentage" : CPIDX(i + 7) = True
            ConfigName(i + 8) = "DOF" & CIDX.ToString & "_Axis3DIR" : CDIDX(i + 8) = True
            ConfigName(i + 9) = "DOF" & CIDX.ToString & "_Axis4Percentage" : CPIDX(i + 9) = True
            ConfigName(i + 10) = "DOF" & CIDX.ToString & "_Axis4DIR" : CDIDX(i + 10) = True
            ConfigName(i + 11) = "DOF" & CIDX.ToString & "_Axis5Percentage" : CPIDX(i + 11) = True
            ConfigName(i + 12) = "DOF" & CIDX.ToString & "_Axis5DIR" : CDIDX(i + 12) = True
            CIDX += 1
            If CIDX >= 6 Then Exit For
        Next

        For i As Integer = 0 To 5
            xConfig.DOF_Type(i) = 0
            xConfig.DOF_Axis0DIR(i) = True
            xConfig.DOF_Axis0Percentage(i) = 0.0
            xConfig.DOF_Axis1DIR(i) = True
            xConfig.DOF_Axis1Percentage(i) = 0.0
            xConfig.DOF_Axis2DIR(i) = True
            xConfig.DOF_Axis2Percentage(i) = 0.0
            xConfig.DOF_Axis3DIR(i) = True
            xConfig.DOF_Axis3Percentage(i) = 0.0
            xConfig.DOF_Axis4DIR(i) = True
            xConfig.DOF_Axis4Percentage(i) = 0.0
            xConfig.DOF_Axis5DIR(i) = True
            xConfig.DOF_Axis5Percentage(i) = 0.0
        Next

        CheckConfigFolder()

        If Not myStorage.FileExists(ConfigFolder & "\\" & DOFConfigFilename) Then
            Debug.WriteLine("create axis config file")
            Dim newReg As New System.IO.StreamWriter(New System.IO.IsolatedStorage.IsolatedStorageFileStream(ConfigFolder & "\\" & DOFConfigFilename, FileMode.Create, FileAccess.Write, myStorage))
            newReg.WriteLine("# available axis type - none , roll , pitch , yaw , heave , surge , away")
            newReg.WriteLine("# available direction - normal , reverse")
            For i As Integer = 0 To ConfigName.Length - 1
                If ConfigName(i) = "" Or CNIDX > 9 Then Exit For
                If CTIDX(i) Then newReg.WriteLine(vbCrLf & ConfigName(i) & "=" & DOFName(CNIDX)) : CNIDX += 1
                If CPIDX(i) Then newReg.WriteLine(ConfigName(i) & "=0")
                If CDIDX(i) Then newReg.WriteLine(ConfigName(i) & "=normal")
            Next
            newReg.Flush()
            newReg.Dispose()
        Else
            Debug.WriteLine("read axis config file...")
            Dim existingFile As New System.IO.StreamReader(New System.IO.IsolatedStorage.IsolatedStorageFileStream(ConfigFolder & "\\" & DOFConfigFilename, FileMode.Open, FileAccess.Read, myStorage))
            Do Until (existingFile.EndOfStream)
                RData = existingFile.ReadLine()
                ConfigID = -1
                For i As Integer = 0 To ConfigName.Length - 1
                    If ConfigName(i) = "" Then Exit For
                    If RData.StartsWith(ConfigName(i), StringComparison.Ordinal) Then
                        ConfigID = i
                        VData = getStrValue(RData).Trim
                        Exit For
                    End If
                Next
                Select Case ConfigID
                    Case 0 To 12 : DOFID = 0
                    Case 13 To 25 : DOFID = 1
                    Case 26 To 38 : DOFID = 2
                    Case 39 To 51 : DOFID = 3
                    Case 52 To 64 : DOFID = 4
                    Case 65 To 77 : DOFID = 5
                    Case Else : DOFID = -1
                End Select
                If DOFID >= 0 Then
                    Select Case ConfigID
                        Case 0, 13, 26, 39, 52, 65 : xConfig.DOF_Type(DOFID) = getAxTypeValue(VData)
                        Case 1, 14, 27, 40, 53, 66 : xConfig.DOF_Axis0Percentage(DOFID) = getIntValue(VData)
                        Case 2, 15, 28, 41, 54, 67 : xConfig.DOF_Axis0DIR(DOFID) = Not IntToBool(getAxDirValue(VData))
                        Case 3, 16, 29, 42, 55, 68 : xConfig.DOF_Axis1Percentage(DOFID) = getIntValue(VData)
                        Case 4, 17, 30, 43, 56, 69 : xConfig.DOF_Axis1DIR(DOFID) = Not IntToBool(getAxDirValue(VData))
                        Case 5, 18, 31, 44, 57, 70 : xConfig.DOF_Axis2Percentage(DOFID) = getIntValue(VData)
                        Case 6, 19, 32, 45, 58, 71 : xConfig.DOF_Axis2DIR(DOFID) = Not IntToBool(getAxDirValue(VData))
                        Case 7, 20, 33, 46, 59, 72 : xConfig.DOF_Axis3Percentage(DOFID) = getIntValue(VData)
                        Case 8, 21, 34, 47, 60, 73 : xConfig.DOF_Axis3DIR(DOFID) = Not IntToBool(getAxDirValue(VData))
                        Case 9, 22, 35, 48, 61, 74 : xConfig.DOF_Axis4Percentage(DOFID) = getIntValue(VData)
                        Case 10, 23, 36, 49, 62, 75 : xConfig.DOF_Axis4DIR(DOFID) = Not IntToBool(getAxDirValue(VData))
                        Case 11, 24, 37, 50, 63, 76 : xConfig.DOF_Axis5Percentage(DOFID) = getIntValue(VData)
                        Case 12, 25, 38, 51, 64, 77 : xConfig.DOF_Axis5DIR(DOFID) = Not IntToBool(getAxDirValue(VData))
                    End Select
                End If
            Loop
            existingFile.Dispose()
            Debug.WriteLine("finish")

            For i As Integer = 0 To 5
                If xConfig.DOF_Type(i) <= 0 Then
                    xConfig.DOF_Axis0DIR(i) = True
                    xConfig.DOF_Axis0Percentage(i) = 0.0
                    xConfig.DOF_Axis1DIR(i) = True
                    xConfig.DOF_Axis1Percentage(i) = 0.0
                    xConfig.DOF_Axis2DIR(i) = True
                    xConfig.DOF_Axis2Percentage(i) = 0.0
                    xConfig.DOF_Axis3DIR(i) = True
                    xConfig.DOF_Axis3Percentage(i) = 0.0
                    xConfig.DOF_Axis4DIR(i) = True
                    xConfig.DOF_Axis4Percentage(i) = 0.0
                    xConfig.DOF_Axis5DIR(i) = True
                    xConfig.DOF_Axis5Percentage(i) = 0.0
                End If
            Next

        End If
        Return xConfig
    End Function

    Public Function LoadOPConfig() As OP_Config
        Const TotalAxis As Integer = 8
        Const TotalProfile As Integer = 20

        Dim ConfigID As Integer = -1
        Dim VData As String = ""

        Dim xConfig As OP_Config

        ReDim xConfig.Axis_Type(10)
        ReDim xConfig.Axis_HomeEnable(10)
        ReDim xConfig.Axis_IndexEnable(10)
        ReDim xConfig.Axis_EndstopEnable(10)
        ReDim xConfig.Axis_EndstopOffset(10)
        ReDim xConfig.Axis_CalibrationProfile(10)
        ReDim xConfig.Axis_GearRatio(10)
        ReDim xConfig.Axis_HomeDeg(10)
        ReDim xConfig.Axis_HomeDir(10)
        ReDim xConfig.Axis_HomeEnc(10)
        ReDim xConfig.Axis_HomeLimit(10)
        ReDim xConfig.Axis_HomeTotalance(10)
        ReDim xConfig.Axis_HomeTimeout(10)
        ReDim xConfig.Axis_IndexTimeout(10)
        ReDim xConfig.Axis_PPR(10)
        ReDim xConfig.Axis_RangePositive(10)
        ReDim xConfig.Axis_RangeNegative(10)
        ReDim xConfig.Axis_RunProfile(10)
        ReDim xConfig.Axis_ParkProfile(10)
        ReDim xConfig.Axis_ConfigOK(10)

        ReDim xConfig.Profile_CurrentLimit(TotalProfile)
        ReDim xConfig.Profile_CurrentLimitTolerance(TotalProfile)
        ReDim xConfig.Profile_CurrentRange(TotalProfile)
        ReDim xConfig.Profile_CurrentControlBandwidth(TotalProfile)
        ReDim xConfig.Profile_VelocityLimit(TotalProfile)
        ReDim xConfig.Profile_TrajectoryVelocityLimit(TotalProfile)
        ReDim xConfig.Profile_AccelerationLimit(TotalProfile)
        ReDim xConfig.Profile_DeaccelerationLimit(TotalProfile)
        ReDim xConfig.Profile_PositionGain(TotalProfile)
        ReDim xConfig.Profile_VelocityGain(TotalProfile)
        ReDim xConfig.Profile_VelocityIntegratorGain(TotalProfile)
        ReDim xConfig.Profile_CalibrationCurrent(TotalProfile)
        ReDim xConfig.Profile_CalibrationAcceleration(TotalProfile)
        ReDim xConfig.Profile_CalibrationVelocity(TotalProfile)
        ReDim xConfig.Profile_CalibrationRamp(TotalProfile)
        ReDim xConfig.Profile_ConfigOK(TotalProfile)

        Dim ConfigName(1000) As String
        Dim LineSpacer(1000) As Boolean
        Dim GroupIndex(1000) As Integer
        Dim GroupConfigIndex(1000) As Integer
        Dim ConfigCheck_Axis(10, 50) As Integer
        Dim ConfigCheck_Profile(TotalProfile, 50) As Integer
        Dim GroupConfigID As Integer = -1
        Dim GroupID As Integer = -1
        Dim CCIDX As Integer = 0
        Dim CNIDX As Integer = 0
        Dim CXIDX As Integer = 0
        Dim xC As Integer = 0
        Dim gStr As String = ""
        Dim AxisGroup_StartIndex As Integer = 0
        Dim AxisGroup_TotalItem As Integer = 0
        Dim ProfileGroup_StartIndex As Integer = 0
        Dim ProfileGroup_TotalItem As Integer = 0
        Dim Misc_TotalItem As Integer = 0
        Dim ConfigGroup_Name(1000) As String

        '--- Axis group ---
        AxisGroup_StartIndex = 0 'zero base array pointer
        AxisGroup_TotalItem = 19
        ConfigGroup_Name(0) = "Type"
        ConfigGroup_Name(1) = "HomeEnable"
        ConfigGroup_Name(2) = "HomeDeg"
        ConfigGroup_Name(3) = "HomeDir"
        ConfigGroup_Name(4) = "HomeEnc"
        ConfigGroup_Name(5) = "HomeLimit"
        ConfigGroup_Name(6) = "HomeTotalance"
        ConfigGroup_Name(7) = "HomeTimeout"
        ConfigGroup_Name(8) = "IndexTimeout"
        ConfigGroup_Name(9) = "PPR"
        ConfigGroup_Name(10) = "RangePositive"
        ConfigGroup_Name(11) = "RangeNegative"
        ConfigGroup_Name(12) = "GearRatio"
        ConfigGroup_Name(13) = "CalibrationProfile"
        ConfigGroup_Name(14) = "RunProfile"
        ConfigGroup_Name(15) = "ParkProfile"
        ConfigGroup_Name(16) = "IndexEnable"
        ConfigGroup_Name(17) = "EndstopEnable"
        ConfigGroup_Name(18) = "EndstopOffset"

        '--- Profile group ---
        ProfileGroup_StartIndex = 19 'zero base array pointer
        ProfileGroup_TotalItem = 15
        ConfigGroup_Name(19) = "CurrentLimitTolerance"
        ConfigGroup_Name(20) = "CurrentLimit"
        ConfigGroup_Name(21) = "CurrentRange"
        ConfigGroup_Name(22) = "CurrentControlBandwidth"
        ConfigGroup_Name(23) = "VelocityLimit"
        ConfigGroup_Name(24) = "TrajectoryVelocityLimit"
        ConfigGroup_Name(25) = "AccelerationLimit"
        ConfigGroup_Name(26) = "DeaccelerationLimit"
        ConfigGroup_Name(27) = "PositionGain"
        ConfigGroup_Name(28) = "VelocityGain"
        ConfigGroup_Name(29) = "VelocityIntegratorGain"
        ConfigGroup_Name(30) = "CalibrationCurrent"
        ConfigGroup_Name(31) = "CalibrationVelocity"
        ConfigGroup_Name(32) = "CalibrationAcceleration"
        ConfigGroup_Name(33) = "CalibrationRamp"

        '--- Misc ---
        Misc_TotalItem = 6
        ConfigName(0) = "Auto_Start"
        ConfigName(1) = "Telemetry_Mode"
        ConfigName(2) = "Telemetry_Interval"
        ConfigName(3) = "Telemetry_Timeout"
        ConfigName(4) = "Delay_ParameterSet"
        ConfigName(5) = "Delay_StateChange"

        System.Array.Clear(ConfigCheck_Axis, 0, ConfigCheck_Axis.Length)
        System.Array.Clear(ConfigCheck_Profile, 0, ConfigCheck_Profile.Length)

        xConfig.Auto_Start = 0
        xConfig.Telemetry_Interval = 0
        xConfig.Telemetry_Mode = 0
        xConfig.Telemetry_Timeout = 50

        For i As Integer = 0 To GroupConfigIndex.Length - 1
            GroupConfigIndex(i) = -1
        Next

        CXIDX = Misc_TotalItem
        CNIDX = 0
        For i As Integer = CXIDX To ConfigName.Length - 1 Step AxisGroup_TotalItem
            LineSpacer(i) = True
            gStr = "Axis" & CNIDX.ToString & "_"
            For j As Integer = i To i + AxisGroup_TotalItem
                xC = AxisGroup_StartIndex + (j - i)
                GroupIndex(j) = CNIDX
                GroupConfigIndex(j) = xC
                ConfigName(j) = gStr & ConfigGroup_Name(xC)
            Next
            CNIDX += 1
            If CNIDX > TotalAxis - 1 Then
                CXIDX = i + AxisGroup_TotalItem
                Exit For
            End If
        Next

        CNIDX = 0
        For i As Integer = CXIDX To ConfigName.Length - 1 Step ProfileGroup_TotalItem
            LineSpacer(i) = True
            gStr = "Profile" & CNIDX.ToString("D2") & "_"
            For j As Integer = i To i + ProfileGroup_TotalItem
                xC = ProfileGroup_StartIndex + (j - i)
                GroupIndex(j) = CNIDX
                GroupConfigIndex(j) = xC
                ConfigName(j) = gStr & ConfigGroup_Name(xC)
            Next
            CNIDX += 1
            If CNIDX > TotalProfile Then Exit For
        Next

        Dim RData As String = ""

        CheckConfigFolder()
        If Not myStorage.FileExists(ConfigFolder & "\\" & OperationConfigFilename) Then
            Debug.WriteLine("create operation config file")
            Dim newFile As New System.IO.StreamWriter(New System.IO.IsolatedStorage.IsolatedStorageFileStream(ConfigFolder & "\\" & OperationConfigFilename, FileMode.Create, FileAccess.Write, myStorage))
            newFile.WriteLine("# available axis type - none , circular , linear , direct")
            newFile.WriteLine("# available direction - normal , reverse")
            newFile.WriteLine("# timeout unit - seconds")
            newFile.WriteLine("")
            For i As Integer = 0 To ConfigName.Length - 1
                If ConfigName(i) = "" Then Exit For
                If Not ConfigName(i).StartsWith("#") Then
                    If LineSpacer(i) Then newFile.WriteLine("")
                    newFile.WriteLine(ConfigName(i) & "=")
                End If
            Next
            newFile.Flush()
            newFile.Dispose()
        Else
            Debug.WriteLine("read operation config file...")

            For i As Integer = 0 To 10
                xConfig.Axis_Type(i) = ErrorValue_Int
                xConfig.Axis_HomeEnable(i) = ErrorValue_Int
                xConfig.Axis_IndexEnable(i) = ErrorValue_Int
                xConfig.Axis_EndstopEnable(i) = ErrorValue_Int
                xConfig.Axis_EndstopOffset(i) = ErrorValue_Int
                xConfig.Axis_HomeDeg(i) = ErrorValue_Single
                xConfig.Axis_HomeDir(i) = False
                xConfig.Axis_HomeEnc(i) = ErrorValue_Int
                xConfig.Axis_HomeLimit(i) = ErrorValue_Int
                xConfig.Axis_HomeTotalance(i) = ErrorValue_Single
                xConfig.Axis_HomeTimeout(i) = ErrorValue_Int
                xConfig.Axis_IndexTimeout(i) = ErrorValue_Int
                xConfig.Axis_PPR(i) = ErrorValue_Int
                xConfig.Axis_RangePositive(i) = ErrorValue_Single
                xConfig.Axis_RangeNegative(i) = ErrorValue_Single
                xConfig.Axis_GearRatio(i) = ErrorValue_Single
                xConfig.Axis_CalibrationProfile(i) = ErrorValue_Int
                xConfig.Axis_RunProfile(i) = ErrorValue_Int
                xConfig.Axis_ParkProfile(i) = ErrorValue_Int
            Next

            For i As Integer = 0 To TotalProfile - 1
                xConfig.Profile_CurrentLimit(i) = ErrorValue_Single
                xConfig.Profile_CurrentLimitTolerance(i) = ErrorValue_Single
                xConfig.Profile_CurrentRange(i) = ErrorValue_Single
                xConfig.Profile_CurrentControlBandwidth(i) = ErrorValue_Single
                xConfig.Profile_VelocityLimit(i) = ErrorValue_Single
                xConfig.Profile_TrajectoryVelocityLimit(i) = ErrorValue_Single
                xConfig.Profile_AccelerationLimit(i) = ErrorValue_Single
                xConfig.Profile_DeaccelerationLimit(i) = ErrorValue_Single
                xConfig.Profile_PositionGain(i) = ErrorValue_Single
                xConfig.Profile_VelocityGain(i) = ErrorValue_Single
                xConfig.Profile_VelocityIntegratorGain(i) = ErrorValue_Single
                xConfig.Profile_CalibrationCurrent(i) = ErrorValue_Single
                xConfig.Profile_CalibrationVelocity(i) = ErrorValue_Single
                xConfig.Profile_CalibrationAcceleration(i) = ErrorValue_Single
                xConfig.Profile_CalibrationRamp(i) = ErrorValue_Single
            Next

            Dim existingFile As New System.IO.StreamReader(New System.IO.IsolatedStorage.IsolatedStorageFileStream(ConfigFolder & "\\" & OperationConfigFilename, FileMode.Open, FileAccess.Read, myStorage))
            Do Until (existingFile.EndOfStream)
                RData = existingFile.ReadLine()
                ConfigID = -1
                GroupConfigID = -1
                For i As Integer = 0 To ConfigName.Length - 1
                    If ConfigName(i) = "" Then Exit For
                    If RData.StartsWith(ConfigName(i), StringComparison.Ordinal) Then
                        ConfigID = i
                        GroupID = GroupIndex(i)
                        GroupConfigID = GroupConfigIndex(i)
                        VData = getStrValue(RData).Trim
                        If VData = "" Then ConfigID = -1
                        Exit For
                    End If
                Next
                Select Case ConfigID

                    Case 0 : xConfig.Auto_Start = getIntValue(VData)
                    Case 1 : xConfig.Telemetry_Mode = getIntValue(VData)
                    Case 2 : xConfig.Telemetry_Interval = getIntValue(VData)
                    Case 3 : xConfig.Telemetry_Timeout = getIntValue(VData)
                    Case 4 : xConfig.Delay_ParameterSet = getIntValue(VData)
                    Case 5 : xConfig.Delay_StateChange = getIntValue(VData)

                End Select
                Select Case GroupConfigID

                    Case 0 : xConfig.Axis_Type(GroupID) = getOutputType(VData) : ConfigCheck_Axis(GroupID, GroupConfigID) += 1'getOutputType(VData)
                    Case 1 : xConfig.Axis_HomeEnable(GroupID) = IntToBool(getIntValue(VData)) : ConfigCheck_Axis(GroupID, GroupConfigID) += testIntValue(VData)
                    Case 2 : xConfig.Axis_HomeDeg(GroupID) = getSingleValue(VData) : ConfigCheck_Axis(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 3 : xConfig.Axis_HomeDir(GroupID) = IntToBool(getAxDirValue(VData)) : ConfigCheck_Axis(GroupID, GroupConfigID) += 1'getAxDirValue(VData)
                    Case 4 : xConfig.Axis_HomeEnc(GroupID) = getIntValue(VData) : ConfigCheck_Axis(GroupID, GroupConfigID) += testIntValue(VData)
                    Case 5 : xConfig.Axis_HomeLimit(GroupID) = getIntValue(VData) : ConfigCheck_Axis(GroupID, GroupConfigID) += testIntValue(VData)
                    Case 6 : xConfig.Axis_HomeTotalance(GroupID) = getSingleValue(VData) : ConfigCheck_Axis(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 7 : xConfig.Axis_HomeTimeout(GroupID) = getIntValue(VData) : ConfigCheck_Axis(GroupID, GroupConfigID) += testIntValue(VData)
                    Case 8 : xConfig.Axis_IndexTimeout(GroupID) = getIntValue(VData) : ConfigCheck_Axis(GroupID, GroupConfigID) += testIntValue(VData)
                    Case 9 : xConfig.Axis_PPR(GroupID) = getIntValue(VData) : ConfigCheck_Axis(GroupID, GroupConfigID) += testIntValue(VData)
                    Case 10 : xConfig.Axis_RangePositive(GroupID) = getSingleValue(VData) : ConfigCheck_Axis(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 11 : xConfig.Axis_RangeNegative(GroupID) = getSingleValue(VData) : ConfigCheck_Axis(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 12 : xConfig.Axis_GearRatio(GroupID) = getSingleValue(VData) : ConfigCheck_Axis(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 13 : xConfig.Axis_CalibrationProfile(GroupID) = getIntValue(VData) : ConfigCheck_Axis(GroupID, GroupConfigID) += testIntValue(VData)
                    Case 14 : xConfig.Axis_RunProfile(GroupID) = getIntValue(VData) : ConfigCheck_Axis(GroupID, GroupConfigID) += testIntValue(VData)
                    Case 15 : xConfig.Axis_ParkProfile(GroupID) = getIntValue(VData) : ConfigCheck_Axis(GroupID, GroupConfigID) += testIntValue(VData)
                    Case 16 : xConfig.Axis_IndexEnable(GroupID) = IntToBool(getIntValue(VData)) : ConfigCheck_Axis(GroupID, GroupConfigID) += testIntValue(VData)
                    Case 17 : xConfig.Axis_EndstopEnable(GroupID) = IntToBool(getIntValue(VData)) : ConfigCheck_Axis(GroupID, GroupConfigID) += testIntValue(VData)
                    Case 18 : xConfig.Axis_EndstopOffset(GroupID) = getSingleValue(VData) : ConfigCheck_Axis(GroupID, GroupConfigID) += testSingleValue(VData)

                    Case 19 : xConfig.Profile_CurrentLimitTolerance(GroupID) = getSingleValue(VData) : ConfigCheck_Profile(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 20 : xConfig.Profile_CurrentLimit(GroupID) = getSingleValue(VData) : ConfigCheck_Profile(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 21 : xConfig.Profile_CurrentRange(GroupID) = getSingleValue(VData) : ConfigCheck_Profile(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 22 : xConfig.Profile_CurrentControlBandwidth(GroupID) = getSingleValue(VData) : ConfigCheck_Profile(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 23 : xConfig.Profile_VelocityLimit(GroupID) = getSingleValue(VData) : ConfigCheck_Profile(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 24 : xConfig.Profile_TrajectoryVelocityLimit(GroupID) = getSingleValue(VData) : ConfigCheck_Profile(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 25 : xConfig.Profile_AccelerationLimit(GroupID) = getSingleValue(VData) : ConfigCheck_Profile(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 26 : xConfig.Profile_DeaccelerationLimit(GroupID) = getSingleValue(VData) : ConfigCheck_Profile(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 27 : xConfig.Profile_PositionGain(GroupID) = getSingleValue(VData) : ConfigCheck_Profile(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 28 : xConfig.Profile_VelocityGain(GroupID) = getSingleValue(VData) : ConfigCheck_Profile(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 29 : xConfig.Profile_VelocityIntegratorGain(GroupID) = getSingleValue(VData) : ConfigCheck_Profile(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 30 : xConfig.Profile_CalibrationCurrent(GroupID) = getSingleValue(VData) : ConfigCheck_Profile(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 31 : xConfig.Profile_CalibrationVelocity(GroupID) = getSingleValue(VData) : ConfigCheck_Profile(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 32 : xConfig.Profile_CalibrationAcceleration(GroupID) = getSingleValue(VData) : ConfigCheck_Profile(GroupID, GroupConfigID) += testSingleValue(VData)
                    Case 33 : xConfig.Profile_CalibrationRamp(GroupID) = getSingleValue(VData) : ConfigCheck_Profile(GroupID, GroupConfigID) += testSingleValue(VData)

                End Select

            Loop
            existingFile.Dispose()
            Dim ConfCheck As Integer = 0

            For i As Integer = 0 To TotalAxis
                ConfCheck = 0
                xConfig.Axis_ConfigOK(i) = True
                For j As Integer = 0 To 50
                    ConfCheck += ConfigCheck_Axis(i, j)
                    If ConfigCheck_Axis(i, j) > 1 Then xConfig.Axis_ConfigOK(i) = False
                Next
                If ConfCheck <> AxisGroup_TotalItem Then xConfig.Axis_ConfigOK(i) = False
            Next

            For i As Integer = 0 To TotalProfile - 1
                ConfCheck = 0
                xConfig.Profile_ConfigOK(i) = True
                For j As Integer = 0 To 50
                    ConfCheck += ConfigCheck_Profile(i, j)
                    If ConfigCheck_Profile(i, j) > 1 Then xConfig.Profile_ConfigOK(i) = False
                Next
                If ConfCheck <> ProfileGroup_TotalItem Then xConfig.Profile_ConfigOK(i) = False
            Next

            Debug.WriteLine("finish")
        End If

        Return xConfig

    End Function

    Private Sub CheckConfigFolder()
        If Not myStorage.DirectoryExists(ConfigFolder) Then
            myStorage.CreateDirectory(ConfigFolder)
            Debug.WriteLine("create config folder")
        End If
    End Sub

    Private Function IntToBool(IntValue As Integer) As Boolean
        Dim res = False
        If IntValue > 0 Then res = True
        Return res
    End Function

    Private Function getAxDirValue(rawStr As String) As Integer
        Dim dict() As String = {"NORMAL", "REVERSE"}
        Dim res As Integer = 0
        For i As Integer = 0 To dict.Length - 1
            If dict(i) = "" Then Exit For
            If rawStr.Trim.ToUpper.StartsWith(dict(i), StringComparison.Ordinal) Then
                res = i
                Exit For
            End If
        Next
        Return res
    End Function

    Private Function getAxTypeValue(rawStr As String) As Integer
        Dim dict() As String = {"NONE", "ROLL", "PITCH", "YAW", "HEAVE", "SURGE", "SWAY"}
        Dim res As Integer = 0
        For i As Integer = 0 To dict.Length - 1
            If dict(i) = "" Then Exit For
            If rawStr.Trim.ToUpper.StartsWith(dict(i), StringComparison.Ordinal) Then
                res = i
                Exit For
            End If
        Next
        Return res
    End Function

    Private Function getOutputType(rawStr As String) As Integer
        Dim dict() As String = {"NONE", "CIRCULAR", "LINEAR", "DIRECT"}
        Dim res As Integer = ErrorValue_Int
        For i As Integer = 0 To dict.Length - 1
            If dict(i) = "" Then Exit For
            If rawStr.Trim.ToUpper.StartsWith(dict(i), StringComparison.Ordinal) Then
                res = i
                Exit For
            End If
        Next
        Return res
    End Function

    Private Function getStrValue(rawStr As String) As String
        Dim res As String = ""
        If rawStr <> "" Then res = rawStr.Substring(rawStr.IndexOf("=") + 1).Trim
        Return res
    End Function

    Private Function getIntValue(rawStr As String) As Integer
        Dim res As Integer = ErrorValue_Int
        Integer.TryParse(rawStr, res)
        If res = 0 And rawStr.Contains("0") = False Then res = ErrorValue_Int
        Return res
    End Function

    Private Function getSingleValue(rawStr As String) As Single
        Dim res As Single = ErrorValue_Single
        Single.TryParse(rawStr, res)
        If res = 0 And rawStr.Contains("0") = False Then res = ErrorValue_Single
        Return res
    End Function

    Private Function testIntValue(rawStr As String) As Integer
        Dim res As Integer = 1
        Dim dummy As Integer = 0
        Integer.TryParse(rawStr, dummy)
        If dummy = 0 And rawStr.Contains("0") = False Then res = 0
        Return res
    End Function

    Private Function testSingleValue(rawStr As String) As Integer
        Dim res As Integer = 1
        Dim dummy As Single = 0
        Single.TryParse(rawStr, dummy)
        If dummy = 0 And rawStr.Contains("0") = False Then res = 0
        Return res
    End Function

End Module

