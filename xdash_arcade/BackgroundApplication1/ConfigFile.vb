Module ConfigFile
    Dim MotionUsage_Val As Integer = 0
    Dim DefaultOutputState_Val As Boolean = False

    Dim ConfigList(1000) As String
    Dim ConfSearch(1000) As String
    Dim ConfCount As Integer = 0

    Public Sub Init()
        System.Array.Clear(ConfigList, 0, ConfigList.Length)
        ConfCount = 0

        ConfigList(0) = "Axis0_HomeDeg"
        ConfigList(1) = "Axis0_HomeDir"
        ConfigList(2) = "Axis1_HomeDeg"
        ConfigList(3) = "Axis1_HomeDir"
        ConfigList(4) = "Axis2_HomeDeg"
        ConfigList(5) = "Axis2_HomeDir"
        ConfigList(6) = "Axis3_HomeDeg"
        ConfigList(7) = "Axis3_HomeDir"
        ConfigList(8) = "PPRTransis_Limit"
        ConfigList(9) = "Home_Timeout"
        ConfigList(10) = "Home_Limit"
        ConfigList(11) = "Home_Totalance"
        ConfigList(12) = "PPR"
        ConfigList(13) = "Axis0_RotationRange"
        ConfigList(14) = "Axis1_RotationRange"
        ConfigList(15) = "Axis2_RotationRange"
        ConfigList(16) = "Axis3_RotationRange"
        ConfigList(17) = "Gear0_Raito"
        ConfigList(18) = "Gear1_Raito"
        ConfigList(19) = "Gear2_Raito"
        ConfigList(20) = "Gear3_Raito"
        ConfigList(21) = "Shaft_Speed"
        ConfigList(22) = "Axis0_CPS"
        ConfigList(23) = "Axis1_CPS"
        ConfigList(24) = "Axis2_CPS"
        ConfigList(25) = "Axis3_CPS"
        ConfigList(26) = "Auto_Start"

        For i As Integer = 0 To ConfSearch.Length - 1
            If ConfigList(i) <> "" Then
                ConfSearch(i) = ConfigList(i).ToUpper
                ConfCount += 1
            Else
                Exit For
            End If
        Next
    End Sub

    Public Sub Read(FileName As String)

    End Sub

    Private Function GetSingleValue(ConfString As String) As Single
        Dim VIDX As Integer = 0
        Dim RValue As Integer = -9999.0
        If ConfString <> "" Then
            VIDX = ConfString.IndexOf("=")
            If VIDX > 0 And ConfString.Length > VIDX Then
                Single.TryParse(ConfString.Substring(VIDX + 1), RValue)
            End If
        End If
        Return RValue
    End Function

End Module
