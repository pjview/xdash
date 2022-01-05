Module Log
    Structure LogStruct
        Dim Detail As String
        Dim CMD As String
        Dim Drive0TX As String
        Dim Drive0RX As String
        Dim Drive1TX As String
        Dim Drive1RX As String
        Dim Drive2TX As String
        Dim Drive2RX As String
        Dim Display1TX As String
        Dim Display1RX As String
        Dim BUSTX As String
        Dim BUSRX As String

        Dim System_Event As String
        Dim System_OPMode As Integer
        Dim Drive0_ON As Integer
        Dim Drive1_ON As Integer
        Dim Drive2_ON As Integer
        Dim Drive3_ON As Integer
        Dim Supply0_Voltage As Integer
        Dim Supply1_Voltage As Integer
        Dim Supply2_Voltage As Integer
        Dim Supply3_Voltage As Integer
        Dim Drive0_Info As String
        Dim Drive1_Info As String
        Dim Drive2_Info As String
        Dim Drive3_Info As String
        Dim Axis_DriverNo() As Integer
        Dim Axis_RawPosition() As Integer
        Dim Axis_StateTXT() As String
        Dim Axis_Temperature() As Integer
        'Dim Axis0_StateTXT As String
        'Dim Axis1_StateTXT As String
        'Dim Axis2_StateTXT As String
        'Dim Axis3_StateTXT As String
        'Dim Axis4_StateTXT As String
        'Dim Axis5_StateTXT As String
        'Dim Axis6_StateTXT As String
        'Dim Axis7_StateTXT As String
        Dim FAN_Output As String
        Dim IR_Output As String

        Dim DOF0_POS As String
        Dim DOF1_POS As String
        Dim DOF2_POS As String
        Dim DOF3_POS As String
        Dim DOF4_POS As String
        Dim DOF5_POS As String

        Dim Axis_CPOS() As Single
        Dim Axis_APOS() As Single
        Dim Axis_RPM() As Single
        Dim Axis_CUR() As Single
        Dim Axis_TEMP() As Single
        Dim Axis_ERR_EXT() As Int16
        Dim Axis_ERR_INT() As Int16

        Dim LoadCell As Single
    End Structure

    Enum AxisDescription As Integer
        None = 0
        Idle = 1
        Run = 2
    End Enum

    Enum DataTag As Byte

        xString = 255
    End Enum

    Public Data As LogStruct
    Private XCount As Integer = 0
    Private LineCount As Integer = 0
    Private TextSpace_FTP As New Text.StringBuilder(32768, 262144)
    Private TextSpace_Local As New Text.StringBuilder(32768, 131072)
    Private TextSpace_Dashboard As New Text.StringBuilder(32768, 131072)
    Private BusyState As Boolean = False
    Private FirstEntry As Boolean = False
    Private LogBusy As New Object
    Private TickOffset As Integer = 0
    Private LogName As String = "Run"
    Private StreamMode As Boolean = True ' --- default stream mode
    Private StreamBusy As Boolean = False
    Private FTPBusy As Boolean = False

    Private Ascii As System.Text.Encoding = System.Text.Encoding.ASCII
    Private LiveTXBuffer(512) As Byte
    Private DashboardTXBuffer(512) As Byte
    Private SessionCount As Integer = 0
    Private SessionName As String = ""
    Private ByteLog(256) As Byte

    'Private Const DefaultIP As String = "192.168.148.160"
    'Private Const DefaultIP As String = "192.168.148.129"
    'Private Const DefaultIP As String = "127.0.0.1"
    'Private Const DefaultPort As Integer = 15544
    'Private Const DefaultPort As Integer = 8001

    Dim LogLock As New Object
    Dim DashboardLock As New Object

    Private Telemetry_EndPoint As System.Net.IPEndPoint
    Private Telemetry_Stream As System.Net.Sockets.UdpClient
    Private Dashboard_EndPoint As System.Net.IPEndPoint
    Private Dashboard_Stream As System.Net.Sockets.UdpClient

    Private Const MachineID As String = "xDash_00"
    Private Const SparatorSymbol As String = ","
    Private Const DoubleTab As String = vbTab & vbTab
    Private Const TimeZone As Integer = 0
    Public Const LOG_Version As String = "0.03.0" '"0.02.2"

    Public Sub Init()
        ReDim Data.Axis_DriverNo(7)
        ReDim Data.Axis_StateTXT(7)
        ReDim Data.Axis_RawPosition(7)
        ReDim Data.Axis_Temperature(7)
        ReDim Data.Axis_APOS(10)
        ReDim Data.Axis_CPOS(10)
        ReDim Data.Axis_RPM(10)
        ReDim Data.Axis_CUR(10)
        ReDim Data.Axis_TEMP(10)
        ReDim Data.Axis_ERR_EXT(10)
        ReDim Data.Axis_ERR_INT(10)

        Data.DOF0_POS = "32767"
        Data.DOF1_POS = "32767"
        Data.DOF2_POS = "32767"
        Data.DOF3_POS = "32767"
        Data.DOF4_POS = "32767"
        Data.DOF5_POS = "32767"
    End Sub

    Public Sub Init_Live(TargetIP As System.Net.IPAddress, SourcePort As UInt16, TargetPort As UInt16)
        Telemetry_EndPoint = New System.Net.IPEndPoint(TargetIP, TargetPort)
        Telemetry_Stream = New System.Net.Sockets.UdpClient(SourcePort)
    End Sub

    Public Sub Init_Dashboard(TargetIP As System.Net.IPAddress, SourcePort As UInt16, TargetPort As UInt16)
        Dashboard_EndPoint = New System.Net.IPEndPoint(TargetIP, TargetPort)
        Dashboard_Stream = New System.Net.Sockets.UdpClient(SourcePort)
    End Sub

    Public Sub Clear()
        FirstEntry = True
        TickOffset = System.Environment.TickCount
        Data.Detail = DateTime.Now.AddHours(TimeZone).ToString
        Data.CMD = "Command"
        Data.Drive0TX = "Drive-0 TX"
        Data.Drive0RX = "Drive-0 RX"
        Data.Drive1TX = "Drive-1 TX"
        Data.Drive1RX = "Drive-1 RX"
        Data.Drive2TX = "Drive-2 TX"
        Data.Drive2RX = "Drive-2 RX"
        Data.Display1TX = "Display TX"
        Data.Display1RX = "Display RX"
        Data.BUSTX = "BUS TX"
        Data.BUSRX = "BUS RX"

        'Data.Axis0_CPOS = "Axis-0 [Request Positition]"
        'Data.Axis0_APOS = "Axis-0 [Actual Positition]"
        'Data.Axis0_RPM = "Axis-0 [RPM (PPR Unit)]"
        'Data.Axis0_CUR = "Axis-0 [Motor Current (A)]"
        'Data.Axis0_TEMP = "Axis-0 [Motor Temperature (C)]"
        'Data.Axis0_ERR_EXT = "Axis-0 [General Error]"

        'Data.Axis1_CPOS = "Axis-1 [Request Positition]"
        'Data.Axis1_APOS = "Axis-1 [Actual Positition]"
        'Data.Axis1_RPM = "Axis-1 [RPM (PPR Unit)]"
        'Data.Axis1_CUR = "Axis-1 [Motor Current (A)]"
        'Data.Axis1_TEMP = "Axis-1 [Motor Temperature (C)]"
        'Data.Axis1_ERR_EXT = "Axis-1 [General Error]"

        'Data.Axis2_CPOS = "Axis-2 [Request Positition]"
        'Data.Axis2_APOS = "Axis-2 [Actual Positition]"
        'Data.Axis2_RPM = "Axis-2 [RPM (PPR Unit)]"
        'Data.Axis2_CUR = "Axis-2 [Motor Current (A)]"
        'Data.Axis2_TEMP = "Axis-2 [Motor Temperature (C)]"
        'Data.Axis2_ERR_EXT = "Axis-2 [General Error]"

        'Data.Axis3_CPOS = "Axis-3 [Request Positition]"
        'Data.Axis3_APOS = "Axis-3 [Actual Positition]"
        'Data.Axis3_RPM = "Axis-3 [RPM (PPR Unit)]"
        'Data.Axis3_CUR = "Axis-3 [Motor Current (A)]"
        'Data.Axis3_TEMP = "Axis-3 [Motor Temperature (C)]"
        'Data.Axis3_ERR_EXT = "Axis-3 [General Error]"

        'Data.Axis4_CPOS = "Axis-4 [Request Positition]"
        'Data.Axis4_APOS = "Axis-4 [Actual Positition]"
        'Data.Axis4_RPM = "Axis-4 [RPM (PPR Unit)]"
        'Data.Axis4_CUR = "Axis-4 [Motor Current (A)]"
        'Data.Axis4_TEMP = "Axis-4 [Motor Temperature (C)]"
        'Data.Axis4_ERR_EXT = "Axis-4 [General Error]"

        'Data.Axis5_CPOS = "Axis-5 [Request Positition]"
        'Data.Axis5_APOS = "Axis-5 [Actual Positition]"
        'Data.Axis5_RPM = "Axis-5 [RPM (PPR Unit)]"
        'Data.Axis5_CUR = "Axis-5 [Motor Current (A)]"
        'Data.Axis5_TEMP = "Axis-5 [Motor Temperature (C)]"
        'Data.Axis5_ERR_EXT = "Axis-5 [General Error]"

        Data.LoadCell = "Axis-0 [Weight/Load (Kg)]"

        TextSpace_Local.Clear()
        TextSpace_FTP.Clear()
        XCount = 0
        LineCount = 0
        Add()

        'Data.Axis0_CPOS = ""
        'Data.Axis0_APOS = ""
        'Data.Axis0_RPM = ""
        'Data.Axis0_CUR = ""
        'Data.Axis0_TEMP = ""
        'Data.Axis0_ERR_EXT = ""
        'Data.Axis0_ERR_INT = ""

        'Data.Axis1_CPOS = ""
        'Data.Axis1_APOS = ""
        'Data.Axis1_RPM = ""
        'Data.Axis1_CUR = ""
        'Data.Axis1_TEMP = ""
        'Data.Axis1_ERR_EXT = ""
        'Data.Axis1_ERR_INT = ""

        'Data.Axis2_CPOS = ""
        'Data.Axis2_APOS = ""
        'Data.Axis2_RPM = ""
        'Data.Axis2_CUR = ""
        'Data.Axis2_TEMP = ""
        'Data.Axis2_ERR_EXT = ""
        'Data.Axis2_ERR_INT = ""

        'Data.Axis3_CPOS = ""
        'Data.Axis3_APOS = ""
        'Data.Axis3_RPM = ""
        'Data.Axis3_CUR = ""
        'Data.Axis3_TEMP = ""
        'Data.Axis3_ERR_EXT = ""
        'Data.Axis3_ERR_INT = ""

        'Data.Axis4_CPOS = ""
        'Data.Axis4_APOS = ""
        'Data.Axis4_RPM = ""
        'Data.Axis4_CUR = ""
        'Data.Axis4_TEMP = ""
        'Data.Axis4_ERR_EXT = ""
        'Data.Axis4_ERR_INT = ""

        'Data.Axis5_CPOS = ""
        'Data.Axis5_APOS = ""
        'Data.Axis5_RPM = ""
        'Data.Axis5_CUR = ""
        'Data.Axis5_TEMP = ""
        'Data.Axis5_ERR_EXT = ""
        'Data.Axis5_ERR_INT = ""

        Data.LoadCell = ""

    End Sub

    Public Sub Add()

        'Exit Sub
        'If BusyState Then Exit Sub
        'BusyState = True

        SyncLock LogLock
            Dim ZDT As DateTime = DateTime.Now.AddHours(TimeZone)
            'Dim DataOut As String = ""
            Dim LogBitstream As LogStruct = Data

            ' ID XX (2 byte 0-1)
            ByteLog(0) = 0
            ByteLog(1) = 0

            ' ID-00 (3 byte 2-4)
            System.Array.Copy(BitConverter.GetBytes(ZDT.Hour), 0, ByteLog, 2, 1)
            System.Array.Copy(BitConverter.GetBytes(ZDT.Minute), 0, ByteLog, 3, 1)
            System.Array.Copy(BitConverter.GetBytes(ZDT.Second), 0, ByteLog, 4, 1)
            'If FirstEntry Then
            '    TextSpace_Local.Append("TimeStamp")
            '    FirstEntry = False
            'Else
            '    TextSpace_Local.Append(ZDT.Hour.ToString("D2"))
            '    TextSpace_Local.Append(":")
            '    TextSpace_Local.Append(ZDT.Minute.ToString("D2"))
            '    TextSpace_Local.Append(":")
            '    TextSpace_Local.Append(ZDT.Second.ToString("D2"))
            'End If
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-01 (4 byte 5-8)
            System.Array.Copy(BitConverter.GetBytes(System.Environment.TickCount - TickOffset), 0, ByteLog, 5, 4)
            'TextSpace_Local.Append((System.Environment.TickCount - TickOffset).ToString)
            'TextSpace.Append(LogMirror.Detail)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-02 
            'TextSpace_Local.Append(LogMirror.CMD)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-03 
            'TextSpace_Local.Append(LogMirror.Drive0TX)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-04
            'TextSpace_Local.Append(LogMirror.Drive0RX)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-05
            'TextSpace_Local.Append(LogMirror.Drive1TX)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-06
            'TextSpace_Local.Append(LogMirror.Drive1RX)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-07
            'TextSpace_Local.Append(LogMirror.Drive2TX)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-08
            'TextSpace_Local.Append(LogMirror.Drive2RX)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-09
            'TextSpace_Local.Append(LogMirror.Display1TX)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-10
            'TextSpace_Local.Append(LogMirror.Display1RX)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-11
            'TextSpace_Local.Append(LogMirror.BUSTX)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-12
            'TextSpace_Local.Append(LogMirror.BUSRX)
            'TextSpace_Local.Append(SparatorSymbol)

            'Data.Detail = ""
            'Data.CMD = ""
            'Data.Drive0TX = ""
            'Data.Drive0RX = ""
            'Data.Drive1TX = ""
            'Data.Drive1RX = ""
            'Data.Drive2TX = ""
            'Data.Drive2RX = ""
            'Data.Display1TX = ""
            'Data.Display1RX = ""
            'Data.BUSTX = ""
            'Data.BUSRX = ""

            ' ID-13 (4 byte 9-12)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_CPOS(0)), 0, ByteLog, 9, 4)
            'TextSpace_Local.Append(LogMirror.Axis0_CPOS)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-14 (4 byte 13-16)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_APOS(0)), 0, ByteLog, 13, 4)
            'TextSpace_Local.Append(LogMirror.Axis0_APOS)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-15 (4 byte 17-20)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_RPM(0)), 0, ByteLog, 17, 4)
            'TextSpace_Local.Append(LogMirror.Axis0_RPM)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-16 (4 byte 21-24)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_CUR(0)), 0, ByteLog, 21, 4)
            'TextSpace_Local.Append(LogMirror.Axis0_CUR)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-17 (4 byte 25-28)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_TEMP(0)), 0, ByteLog, 25, 4)
            'TextSpace.Append(LogMirror.Axis0_TEMP)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-18.A (2 byte 29-30)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_ERR_INT(0)), 0, ByteLog, 29, 2)

            ' ID-18.B (2 byte 31-32)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_ERR_EXT(0)), 0, ByteLog, 31, 2)
            'TextSpace_Local.Append(LogMirror.Axis5_ERR_INT)
            'TextSpace_Local.Append(LogMirror.Axis0_ERR_EXT)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-19 (4 byte 33-36)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_CPOS(1)), 0, ByteLog, 33, 4)
            'TextSpace_Local.Append(LogMirror.Axis1_CPOS)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-20 (4 byte 37-40)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_APOS(1)), 0, ByteLog, 37, 4)
            'TextSpace_Local.Append(LogMirror.Axis1_APOS)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-21 (4 byte 41-44)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_RPM(1)), 0, ByteLog, 41, 4)
            'TextSpace_Local.Append(LogMirror.Axis1_RPM)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-22 (4 byte 45-48)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_CUR(1)), 0, ByteLog, 45, 4)
            'TextSpace_Local.Append(LogMirror.Axis1_CUR)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-23 (4 byte 49-52)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_TEMP(1)), 0, ByteLog, 49, 4)
            'TextSpace.Append(LogMirror.Axis1_TEMP)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-24.A (2 byte 53-54)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_ERR_INT(1)), 0, ByteLog, 53, 2)
            ' ID-24.B (2 byte 55-56)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_ERR_EXT(1)), 0, ByteLog, 55, 2)
            'TextSpace_Local.Append(LogMirror.Axis5_ERR_INT)
            'TextSpace_Local.Append(LogMirror.Axis1_ERR_EXT)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-25 (4 byte 57-60)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_CPOS(2)), 0, ByteLog, 57, 4)
            'TextSpace_Local.Append(LogMirror.Axis2_CPOS)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-26 (4 byte 61-64)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_APOS(2)), 0, ByteLog, 61, 4)
            'TextSpace_Local.Append(LogMirror.Axis2_APOS)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-27 (4 byte 65-68)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_RPM(2)), 0, ByteLog, 65, 4)
            'TextSpace_Local.Append(LogMirror.Axis2_RPM)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-28 (4 byte 69-72)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_CUR(2)), 0, ByteLog, 69, 4)
            'TextSpace_Local.Append(LogMirror.Axis2_CUR)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-29 (4 byte 73-76)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_TEMP(2)), 0, ByteLog, 73, 4)
            'TextSpace.Append(LogMirror.Axis2_TEMP)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-30.A (2 byte 77-78)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_ERR_INT(2)), 0, ByteLog, 77, 2)
            ' ID-30.B (2 byte 79-80)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_ERR_EXT(2)), 0, ByteLog, 79, 2)
            'TextSpace_Local.Append(LogMirror.Axis5_ERR_INT)
            'TextSpace_Local.Append(LogMirror.Axis2_ERR_EXT)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-31 (4 byte 81-84)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_CPOS(3)), 0, ByteLog, 81, 4)
            'TextSpace_Local.Append(LogMirror.Axis3_CPOS)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-32 (4 byte 85-88)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_APOS(3)), 0, ByteLog, 85, 4)
            'TextSpace_Local.Append(LogMirror.Axis3_APOS)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-33 (4 byte 89-92)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_RPM(3)), 0, ByteLog, 89, 4)
            'TextSpace_Local.Append(LogMirror.Axis3_RPM)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-34 (4 byyte 93-96)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_CUR(3)), 0, ByteLog, 93, 4)
            'TextSpace_Local.Append(LogMirror.Axis3_CUR)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-35 (4 byte 97-100)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_TEMP(3)), 0, ByteLog, 97, 4)
            'TextSpace.Append(LogMirror.Axis3_TEMP)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-36.A (2 byte 101-102)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_ERR_INT(3)), 0, ByteLog, 101, 2)
            ' ID-36.B (2 byte 103-104)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_ERR_EXT(3)), 0, ByteLog, 103, 2)
            'TextSpace_Local.Append(LogMirror.Axis5_ERR_INT)
            'TextSpace_Local.Append(LogMirror.Axis3_ERR_EXT)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-37 (4 byte 105-108)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_CPOS(4)), 0, ByteLog, 105, 4)
            'TextSpace_Local.Append(LogMirror.Axis4_CPOS)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-38 (4 byte 109-112)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_APOS(4)), 0, ByteLog, 109, 4)
            'TextSpace_Local.Append(LogMirror.Axis4_APOS)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-39 (4 byte 113-116)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_RPM(4)), 0, ByteLog, 113, 4)
            'TextSpace_Local.Append(LogMirror.Axis4_RPM)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-40 (4 byte 117-120)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_CUR(4)), 0, ByteLog, 117, 4)
            'TextSpace_Local.Append(LogMirror.Axis4_CUR)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-41 (4 byte 121-124)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_TEMP(4)), 0, ByteLog, 121, 4)
            'TextSpace.Append(LogMirror.Axis4_TEMP)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-42.A (4 Byte 125-126)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_ERR_INT(4)), 0, ByteLog, 125, 2)
            ' ID-42.B (2 byte 127-128)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_ERR_EXT(4)), 0, ByteLog, 127, 2)
            'TextSpace_Local.Append(LogMirror.Axis5_ERR_INT)
            'TextSpace_Local.Append(LogMirror.Axis4_ERR_EXT)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-43 (4 byte 129-132)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_CPOS(5)), 0, ByteLog, 129, 4)
            'TextSpace_Local.Append(LogMirror.Axis5_CPOS)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-44 (4 byte 133-136)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_APOS(5)), 0, ByteLog, 133, 4)
            'TextSpace_Local.Append(LogMirror.Axis5_APOS)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-45 (4 byte 137-140)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_RPM(5)), 0, ByteLog, 137, 4)
            'TextSpace_Local.Append(LogMirror.Axis5_RPM)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-46 (4 byte 141-144)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_CUR(5)), 0, ByteLog, 141, 4)
            'TextSpace_Local.Append(LogMirror.Axis5_CUR)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-47 (4 byte 145-148)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_TEMP(5)), 0, ByteLog, 145, 4)
            'TextSpace.Append(LogMirror.Axis5_TEMP)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-48.A (2 byte 149-150)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_ERR_INT(4)), 0, ByteLog, 149, 2)
            ' ID-48.B (2 byte 151-152)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.Axis_ERR_EXT(4)), 0, ByteLog, 151, 2)
            'TextSpace_Local.Append(LogMirror.Axis5_ERR_INT)
            'TextSpace_Local.Append(LogMirror.Axis5_ERR_EXT)
            'TextSpace_Local.Append(SparatorSymbol)

            ' ID-49 (4 byte 153-156)
            System.Array.Copy(BitConverter.GetBytes(LogBitstream.LoadCell), 0, ByteLog, 153, 4)
            'TextSpace_Local.Append(LogMirror.LoadCell)
            'TextSpace_Local.Append(vbCrLf)

            'Data.Axis0_ERR_INT = ""
            'Data.Axis1_ERR_INT = ""
            'Data.Axis2_ERR_INT = ""
            'Data.Axis3_ERR_INT = ""
            'Data.Axis4_ERR_INT = ""

            'If StreamBusy = False Then
            '    StreamBusy = True
            '    If Telemetry_Stream IsNot Nothing Then
            '        'LiveTXBuffer = Ascii.GetBytes(TextSpace_Local.ToString)
            '        'Telemetry_Stream.SendAsync(LiveTXBuffer, LiveTXBuffer.Length, Telemetry_EndPoint)
            '        Telemetry_Stream.SendAsync(ByteLog, 154, Telemetry_EndPoint).Wait()
            '    End If
            '    StreamBusy = False
            'End If

            Telemetry_Stream.SendAsync(ByteLog, ByteLog.Length, Telemetry_EndPoint).Wait()

            'XCount += 1
            'LineCount += 1
            'TextSpace_FTP.Append(TextSpace_Local.ToString)
            'TextSpace_Local.Clear()

            'If XCount > 20 Then
            '    XCount = 0
            '    If Not FTPBusy Then
            '        FTPBusy = True
            '        FTP.FileSend(TextSpace_FTP.ToString)
            '        FTPBusy = False
            '    End If
            '    TextSpace_FTP.Clear()
            'End If

            'If LineCount > 60000 Then
            '    Debug.WriteLine("maximum line limit (split log)")
            '    If FTP.Connected Then
            '        If FTP.FileInProgress Then
            '            Task.Delay(50).Wait()
            '            Debug.WriteLine("Flush buffer")
            '            Flush()
            '            Debug.WriteLine("Close log file")
            '            FTP.FileClose()
            '        End If
            '        Task.Delay(100).Wait()
            '        If Not FTP.FileInProgress Then
            '            Debug.WriteLine("Create log file")
            '            FTP.FileCreate(getName(False), True, False).Wait()
            '            Clear()
            '            Task.Delay(250).Wait()
            '            FTP.FileSend(LOG_Version.ToString & " #LOG Version" & vbCrLf)
            '        End If
            '    End If
            '    TextSpace_Local.Clear()
            '    XCount = 0
            '    LineCount = 0
            'End If
        End SyncLock



        'BusyState = False
    End Sub

    Public Async Sub Dashboard_Update()

        TextSpace_Dashboard.Clear()

        'ID-00 (data version)
        TextSpace_Dashboard.Append(LOG_Version)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-01 (System status - text description)
        TextSpace_Dashboard.Append(Data.System_Event)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-02 (Run status)
        TextSpace_Dashboard.Append(Data.System_OPMode.ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-03 (Drive 0 status - ON/OFF)
        TextSpace_Dashboard.Append(Data.Drive0_ON.ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-04 (Drive 1 status - ON/OFF)
        TextSpace_Dashboard.Append(Data.Drive1_ON.ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-05 (Drive 2 status - ON/OFF)
        TextSpace_Dashboard.Append(Data.Drive2_ON.ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-06 (Drive 3 status - ON/OFF)
        TextSpace_Dashboard.Append(Data.Drive3_ON.ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-07 (Powersupply 0 Voltage)
        TextSpace_Dashboard.Append(Data.Supply0_Voltage.ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-08 (Powersupply 1 Voltage)
        TextSpace_Dashboard.Append(Data.Supply1_Voltage.ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-09 (Powersupply 2 Voltage)
        TextSpace_Dashboard.Append(Data.Supply2_Voltage.ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-10 (Powersupply 3 Voltage)
        TextSpace_Dashboard.Append(Data.Supply3_Voltage.ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-11 (Drive 0 temperature)
        TextSpace_Dashboard.Append("-32768")
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-12 (Drive 1 temperature)
        TextSpace_Dashboard.Append("-32768")
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-13 (Drive 2 temperature)
        TextSpace_Dashboard.Append("-32768")
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-14 (Drive 3 temperature)
        TextSpace_Dashboard.Append("-32768")
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-15 (Axis 0 assosiate driver)
        TextSpace_Dashboard.Append(Data.Axis_DriverNo(0).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-16 (Axis 1 assosiate driver)
        TextSpace_Dashboard.Append(Data.Axis_DriverNo(1).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-17 (Axis 2 assosiate driver)
        TextSpace_Dashboard.Append(Data.Axis_DriverNo(2).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-18 (Axis 3 assosiate driver)
        TextSpace_Dashboard.Append(Data.Axis_DriverNo(3).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-19 (Axis 4 assosiate driver)
        TextSpace_Dashboard.Append(Data.Axis_DriverNo(4).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-20 (Axis 5 assosiate driver)
        TextSpace_Dashboard.Append(Data.Axis_DriverNo(5).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-21 (Axis 6 assosiate driver)
        TextSpace_Dashboard.Append(Data.Axis_DriverNo(6).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-22 (Axis 7 assosiate driver)
        TextSpace_Dashboard.Append(Data.Axis_DriverNo(7).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-23 (Axis 0 status - text description)
        TextSpace_Dashboard.Append(Data.Axis_StateTXT(0))
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-24 (Axis 1 status - text description)
        TextSpace_Dashboard.Append(Data.Axis_StateTXT(1))
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-25 (Axis 2 status - text description)
        TextSpace_Dashboard.Append(Data.Axis_StateTXT(2))
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-26 (Axis 3 status - text description)
        TextSpace_Dashboard.Append(Data.Axis_StateTXT(3))
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-27 (Axis 4 status - text description)
        TextSpace_Dashboard.Append(Data.Axis_StateTXT(4))
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-28 (Axis 5 status - text description)
        TextSpace_Dashboard.Append(Data.Axis_StateTXT(5))
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-29 (Axis 6 status - text description)
        TextSpace_Dashboard.Append(Data.Axis_StateTXT(6))
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-30 (Axis 7 status - text description)
        TextSpace_Dashboard.Append(Data.Axis_StateTXT(7))
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-31 (Axis 0 status - motor position)
        TextSpace_Dashboard.Append(Data.Axis_RawPosition(0).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-32 (Axis 1 status - motor position)
        TextSpace_Dashboard.Append(Data.Axis_RawPosition(1).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-33 (Axis 2 status - motor position)
        TextSpace_Dashboard.Append(Data.Axis_RawPosition(2).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-34 (Axis 3 status - motor position)
        TextSpace_Dashboard.Append(Data.Axis_RawPosition(3).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-35 (Axis 4 status - motor position)
        TextSpace_Dashboard.Append(Data.Axis_RawPosition(4).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-36 (Axis 5 status - motor position)
        TextSpace_Dashboard.Append(Data.Axis_RawPosition(5).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-37 (Axis 6 status - motor position)
        TextSpace_Dashboard.Append(Data.Axis_RawPosition(6).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-38 (Axis 7 status - motor position)
        TextSpace_Dashboard.Append(Data.Axis_RawPosition(7).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)


        '---- temperature ----

        'ID-39 (Axis 0 temperature)
        TextSpace_Dashboard.Append("-32768")
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-40 (Axis 1 temperature)
        TextSpace_Dashboard.Append("-32768")
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-41 (Axis 2 temperature)
        TextSpace_Dashboard.Append("-32768")
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-42 (Axis 3 temperature)
        TextSpace_Dashboard.Append("-32768")
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-43 (Axis 4 temperature)
        TextSpace_Dashboard.Append("-32768")
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-44 (Axis 5 temperature)
        TextSpace_Dashboard.Append("-32768")
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-45 (Axis 6 temperature)
        TextSpace_Dashboard.Append("-32768")
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-46 (Axis 7 temperature)
        TextSpace_Dashboard.Append("-32768")
        TextSpace_Dashboard.Append(SparatorSymbol)

        '---- Position ----

        'ID-47 (DOF 0 Position)
        TextSpace_Dashboard.Append(Data.DOF0_POS)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-48 (DOF 1 Position)
        TextSpace_Dashboard.Append(Data.DOF1_POS)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-49 (DOF 2 Position)
        TextSpace_Dashboard.Append(Data.DOF2_POS)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-50 (DOF 3 Position)
        TextSpace_Dashboard.Append(Data.DOF3_POS)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-51 (DOF 4 Position)
        TextSpace_Dashboard.Append(Data.DOF4_POS)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-52 (DOF 5 Position)
        TextSpace_Dashboard.Append(Data.DOF5_POS)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-53 (Fan Output 1-6)
        TextSpace_Dashboard.Append(Data.FAN_Output)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-54 (Heater Outpput 1-6)
        TextSpace_Dashboard.Append(Data.IR_Output)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-55 (Drive 0 Infomation)
        TextSpace_Dashboard.Append(Data.Drive0_Info)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-56 (Drive 1 Infomation)
        TextSpace_Dashboard.Append(Data.Drive1_Info)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-57 (Drive 2 Infomation)
        TextSpace_Dashboard.Append(Data.Drive2_Info)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-58 (Drive 3 Infomation)
        TextSpace_Dashboard.Append(Data.Drive3_Info)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-59 (loadcell)
        TextSpace_Dashboard.Append(Data.LoadCell.ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        '---- current ----

        'ID-60 (Axis 0 status - motor current)
        TextSpace_Dashboard.Append(Data.Axis_CUR(0).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-61 (Axis 1 status - motor current)
        TextSpace_Dashboard.Append(Data.Axis_CUR(1).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-62 (Axis 2 status - motor current)
        TextSpace_Dashboard.Append(Data.Axis_CUR(2).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-63 (Axis 3 status - motor current)
        TextSpace_Dashboard.Append(Data.Axis_CUR(3).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-64 (Axis 4 status - motor current)
        TextSpace_Dashboard.Append(Data.Axis_CUR(4).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-65 (Axis 5 status - motor current)
        TextSpace_Dashboard.Append(Data.Axis_CUR(5).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-66 (Axis 6 status - motor current)
        TextSpace_Dashboard.Append(Data.Axis_CUR(6).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        'ID-67 (Axis 7 status - motor current)
        TextSpace_Dashboard.Append(Data.Axis_CUR(7).ToString)
        TextSpace_Dashboard.Append(SparatorSymbol)

        TextSpace_Dashboard.Append("*")
        TextSpace_Dashboard.Append(vbCrLf)

        If Dashboard_Stream IsNot Nothing Then
            DashboardTXBuffer = Ascii.GetBytes(TextSpace_Dashboard.ToString)
            Await Dashboard_Stream.SendAsync(DashboardTXBuffer, DashboardTXBuffer.Length, Dashboard_EndPoint)
        End If
        Data.System_Event = ""
    End Sub

    Public Async Sub Flush()
        Await FTP.FileSend(TextSpace_Local.ToString)
        TextSpace_Local.Clear()
        XCount = 0
    End Sub



    Public Function GetDateTimeName() As String
        Dim ZDT As DateTime
        Dim Res As String = ""
        ZDT = DateTime.Now.AddHours(TimeZone)
        Res = ZDT.Year.ToString("D4")
        Res = Res & ZDT.Month.ToString("D2")
        Res = Res & ZDT.Day.ToString("D2")
        Res = Res & "-"
        Res = Res & ZDT.Hour.ToString("D2")
        Res = Res & ZDT.Minute.ToString("D2")
        Res = Res & ZDT.Second.ToString("D2")
        Return Res
    End Function

    Public Sub setName(Name As String)
        If Name.Trim <> "" Then
            LogName = Name.Trim
        End If
    End Sub

    Public Function getStreamMode() As Boolean
        Return StreamMode
    End Function

    Public Sub setStream(Mode As Boolean)
        StreamMode = Mode
    End Sub

    'Public Function getName() As String
    '    Return LogName
    'End Function

    Public Function getName(NewSession As Boolean) As String
        Dim LogName As String = ""
        SessionCount += 1
        If NewSession Or SessionName = "" Then
            SessionCount = 0
            SessionName = GetDateTimeName() & "_" & MachineID & "_Nolimit" & LogName
            LogName = SessionName & ".txt"
        End If
        If SessionCount > 0 Then
            LogName = SessionName & "_" & SessionCount.ToString("D2") & ".txt"
        End If
        Return LogName
    End Function
End Module
