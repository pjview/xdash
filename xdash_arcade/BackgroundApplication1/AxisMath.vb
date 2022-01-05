Module AxisMath

    Structure DOFStruct
        Dim Type As Integer
        Dim Axis0DIR As Boolean
        Dim Axis0Percentage As Double
        Dim Axis1DIR As Boolean
        Dim Axis1Percentage As Double
        Dim Axis2DIR As Boolean
        Dim Axis2Percentage As Double
        Dim Axis3DIR As Boolean
        Dim Axis3Percentage As Double
        Dim Axis4DIR As Boolean
        Dim Axis4Percentage As Double
        Dim Axis5DIR As Boolean
        Dim Axis5Percentage As Double
    End Structure

    Structure AxisStruct
        Dim A0 As Double
        Dim A1 As Double
        Dim A2 As Double
        Dim A3 As Double
        Dim A4 As Double
        Dim A5 As Double
    End Structure

    Public Enum DOFType As Integer
        None = 0
        Roll = 1
        Pitch = 2
        Yaw = 3
        Heave = 4
        Surge = 5
        sway = 6
    End Enum

    Private Const Normal As Boolean = True
    Private Const Reverse As Boolean = False

    '--- interpolation value (1 = disable)
    Private TRoll As Double = 1
    Private TPitch As Double = 1 '0.16
    Private TYaw As Double = 1
    Private THeave As Double = 1
    Private TSurge As Double = 1
    Private TSway As Double = 1

    '--- interpolation zoning (inpercentage)
    Private ITYaw As Double = 80

    Public DOF(6) As DOFStruct
    Public AxisUsage As AxisStruct
    Private AxisShadow As AxisStruct
    Private MotionUsage As Double = 0.5

    'Public OutputScale As Double = ((2 ^ 16) / 2) / 100 '--- for odrive only
    'Public OutputOffset As Integer = (2 ^ 16) / 2       '--- for odrive only

    Private InputOffset As Double = 100.0
    Private InputScale As Double = InputOffset / 100.0

    Private RollShadow As Double = 0.0
    Private PitchShadow As Double = 0.0
    Private YawShadow As Double = 0.0
    Private HeaveShadow As Double = 0.0
    Private SurgeShadow As Double = 0.0
    Private SwayShadow As Double = 0.0

    Private Simtools As Boolean = True

    Private Const AxisMax As Double = 100.0
    Private Const AxisMin As Double = -100.0
    Private Const DistLimit As Double = 25

    Public Function getTypeName(intType As Integer) As String
        Dim res As String = ""
        Select Case intType
            Case DOFType.None : res = "None"
            Case DOFType.Roll : res = "Roll"
            Case DOFType.Pitch : res = "Pitch"
            Case DOFType.Yaw : res = "Yaw"
            Case DOFType.Heave : res = "Heave"
            Case DOFType.Surge : res = "Surge"
            Case DOFType.sway : res = "Sway"
            Case Else : res = "ERROR"
        End Select
        Return res
    End Function

    Public Function getDIRName(boolDIR As Boolean) As String
        Dim res As String = ""
        If boolDIR Then
            res = "Normal"
        Else
            res = "Reverse"
        End If
        Return res
    End Function

    Public Function DOFConfigCheck() As Boolean
        Dim DCount(10) As Integer
        System.Array.Clear(DCount, 0, DCount.Length)
        Debug.WriteLine(DOF.Length)
        Return False
    End Function

    Public Sub DashDash_Init()

        InputOffset = 32768
        InputScale = InputOffset / 100.0

        AxisUsage.A0 = 1.0
        AxisUsage.A1 = 1.0
        AxisUsage.A2 = 0.0
        AxisUsage.A3 = 0.0
        AxisUsage.A4 = 0.0
        AxisUsage.A5 = 0.0

        DOF(0).Type = DOFType.Roll
        DOF(0).Axis0Percentage = 100
        DOF(0).Axis0DIR = True
        DOF(0).Axis1Percentage = 100
        DOF(0).Axis1DIR = False

        DOF(1).Type = DOFType.Pitch
        DOF(1).Axis0Percentage = 100
        DOF(1).Axis0DIR = True
        DOF(1).Axis1Percentage = 100
        DOF(1).Axis1DIR = True

        DOF(2).Type = DOFType.Surge
        DOF(2).Axis0Percentage = 20
        DOF(2).Axis0DIR = True
        DOF(2).Axis1Percentage = 20
        DOF(2).Axis1DIR = True

        DOF(3).Type = DOFType.sway
        DOF(3).Axis0Percentage = 30
        DOF(3).Axis0DIR = True
        DOF(3).Axis1Percentage = 30
        DOF(3).Axis1DIR = False

        DOF(4).Type = DOFType.Heave
        DOF(4).Axis0Percentage = 30
        DOF(4).Axis0DIR = True
        DOF(4).Axis1Percentage = 30
        DOF(4).Axis1DIR = True

        'DOF(0).Type = Roll
        'DOF(0).Axis1Percentage = 80
        'DOF(0).Axis1DIR = False
        'DOF(0).Axis2Percentage = 80
        'DOF(0).Axis2DIR = True

        'DOF(1).Type = Pitch
        'DOF(1).Axis1Percentage = 80
        'DOF(1).Axis1DIR = False
        'DOF(1).Axis2Percentage = 80
        'DOF(1).Axis2DIR = False

        'DOF(2).Type = Surge
        'DOF(2).Axis1Percentage = 20
        'DOF(2).Axis1DIR = False
        'DOF(2).Axis2Percentage = 20
        'DOF(2).Axis2DIR = False

        'DOF(3).Type = Sway
        'DOF(3).Axis1Percentage = 30
        'DOF(3).Axis1DIR = False
        'DOF(3).Axis2Percentage = 30
        'DOF(3).Axis2DIR = True

        'DOF(4).Type = Heave
        'DOF(4).Axis1Percentage = 30
        'DOF(4).Axis1DIR = False
        'DOF(4).Axis2Percentage = 30
        'DOF(4).Axis2DIR = False

        DOF(5).Type = DOFType.None

    End Sub

    Public Sub Default_Init()
        InputOffset = 32768.0
        InputScale = InputOffset / 100.0

        '--- axis output usage ---
        AxisUsage.A0 = 1.0
        AxisUsage.A1 = 1.0
        AxisUsage.A2 = 1.0
        AxisUsage.A3 = 1.0
        AxisUsage.A4 = 1.0
        AxisUsage.A5 = 1.0
    End Sub

    Public Sub DashDash_2Axis_Init()

        InputOffset = 32768.0
        InputScale = InputOffset / 100.0

        '--- axis output usage ---
        AxisUsage.A0 = 1.0
        AxisUsage.A1 = 1.0
        AxisUsage.A2 = 1.0
        AxisUsage.A3 = 1.0
        AxisUsage.A4 = 0.0
        AxisUsage.A5 = 1.0

        DOF(0).Type = DOFType.Roll
        DOF(0).Axis0Percentage = 0
        DOF(0).Axis0DIR = Normal
        DOF(0).Axis1Percentage = 0
        DOF(0).Axis1DIR = Normal
        DOF(0).Axis2Percentage = 100
        DOF(0).Axis2DIR = Normal
        DOF(0).Axis3Percentage = 0
        DOF(0).Axis3DIR = Normal
        DOF(0).Axis4Percentage = 0
        DOF(0).Axis4DIR = Normal
        DOF(0).Axis5Percentage = 0
        DOF(0).Axis5DIR = Normal

        DOF(1).Type = DOFType.Pitch
        DOF(1).Axis0Percentage = 100
        DOF(1).Axis0DIR = Normal
        DOF(1).Axis1Percentage = 0
        DOF(1).Axis1DIR = Normal
        DOF(1).Axis2Percentage = 0
        DOF(1).Axis2DIR = Normal
        DOF(1).Axis3Percentage = 0
        DOF(1).Axis3DIR = Normal
        DOF(1).Axis4Percentage = 0
        DOF(1).Axis4DIR = Normal
        DOF(1).Axis5Percentage = 0
        DOF(1).Axis5DIR = Normal

        DOF(2).Type = DOFType.Surge
        DOF(2).Axis0Percentage = 0
        DOF(2).Axis0DIR = Normal
        DOF(2).Axis1Percentage = 0
        DOF(2).Axis1DIR = Normal
        DOF(2).Axis2Percentage = 0
        DOF(2).Axis2DIR = Normal
        DOF(2).Axis3Percentage = 0
        DOF(2).Axis3DIR = Normal
        DOF(2).Axis4Percentage = 0
        DOF(2).Axis4DIR = Normal
        DOF(2).Axis5Percentage = 0
        DOF(2).Axis5DIR = Normal

        DOF(3).Type = DOFType.sway
        DOF(3).Axis0Percentage = 0
        DOF(3).Axis0DIR = Normal
        DOF(3).Axis1Percentage = 0
        DOF(3).Axis1DIR = Reverse
        DOF(3).Axis2Percentage = 0
        DOF(3).Axis2DIR = Normal
        DOF(3).Axis3Percentage = 0
        DOF(3).Axis3DIR = Reverse
        DOF(3).Axis4Percentage = 0
        DOF(3).Axis4DIR = Normal
        DOF(3).Axis5Percentage = 0
        DOF(3).Axis5DIR = Normal

        DOF(4).Type = DOFType.Heave
        DOF(4).Axis0Percentage = 0
        DOF(4).Axis0DIR = Normal
        DOF(4).Axis1Percentage = 0
        DOF(4).Axis1DIR = Normal
        DOF(4).Axis2Percentage = 0
        DOF(4).Axis2DIR = Normal
        DOF(4).Axis3Percentage = 0
        DOF(4).Axis3DIR = Normal
        DOF(4).Axis4Percentage = 0
        DOF(4).Axis4DIR = Normal
        DOF(4).Axis5Percentage = 0
        DOF(4).Axis5DIR = Normal

        DOF(5).Type = DOFType.Yaw
        DOF(5).Axis0Percentage = 0
        DOF(5).Axis0DIR = Normal
        DOF(5).Axis1Percentage = 100
        DOF(5).Axis1DIR = Normal
        DOF(5).Axis2Percentage = 0
        DOF(5).Axis2DIR = Normal
        DOF(5).Axis3Percentage = 0
        DOF(5).Axis3DIR = Normal
        DOF(5).Axis4Percentage = 0
        DOF(5).Axis4DIR = Normal
        DOF(5).Axis5Percentage = 0
        DOF(5).Axis5DIR = Normal
    End Sub

    Public Sub DashDash_4Axis_Init()

        InputOffset = 32768.0
        InputScale = InputOffset / 100.0

        '--- axis output usage ---
        AxisUsage.A0 = 1.0
        AxisUsage.A1 = 1.0
        AxisUsage.A2 = 1.0
        AxisUsage.A3 = 1.0
        AxisUsage.A4 = 0.0
        AxisUsage.A5 = 1.0

        DOF(0).Type = DOFType.Roll
        DOF(0).Axis0Percentage = 100
        DOF(0).Axis0DIR = Reverse
        DOF(0).Axis1Percentage = 100
        DOF(0).Axis1DIR = Normal
        DOF(0).Axis2Percentage = 0
        DOF(0).Axis2DIR = Normal
        DOF(0).Axis3Percentage = 0
        DOF(0).Axis3DIR = Normal
        DOF(0).Axis4Percentage = 0
        DOF(0).Axis4DIR = Normal
        DOF(0).Axis5Percentage = 0
        DOF(0).Axis5DIR = Normal

        DOF(1).Type = DOFType.Pitch
        DOF(1).Axis0Percentage = 100
        DOF(1).Axis0DIR = Reverse
        DOF(1).Axis1Percentage = 100
        DOF(1).Axis1DIR = Reverse
        DOF(1).Axis2Percentage = 100
        DOF(1).Axis2DIR = Normal
        DOF(1).Axis3Percentage = 0
        DOF(1).Axis3DIR = Normal
        DOF(1).Axis4Percentage = 0
        DOF(1).Axis4DIR = Normal
        DOF(1).Axis5Percentage = 0
        DOF(1).Axis5DIR = Normal

        DOF(2).Type = DOFType.Surge
        DOF(2).Axis0Percentage = 0
        DOF(2).Axis0DIR = Normal
        DOF(2).Axis1Percentage = 0
        DOF(2).Axis1DIR = Normal
        DOF(2).Axis2Percentage = 0
        DOF(2).Axis2DIR = Normal
        DOF(2).Axis3Percentage = 0
        DOF(2).Axis3DIR = Normal
        DOF(2).Axis4Percentage = 0
        DOF(2).Axis4DIR = Normal
        DOF(2).Axis5Percentage = 0
        DOF(2).Axis5DIR = Normal

        DOF(3).Type = DOFType.sway
        DOF(3).Axis0Percentage = 0
        DOF(3).Axis0DIR = Normal
        DOF(3).Axis1Percentage = 0
        DOF(3).Axis1DIR = Reverse
        DOF(3).Axis2Percentage = 0
        DOF(3).Axis2DIR = Normal
        DOF(3).Axis3Percentage = 100
        DOF(3).Axis3DIR = Reverse
        DOF(3).Axis4Percentage = 0
        DOF(3).Axis4DIR = Normal
        DOF(3).Axis5Percentage = 0
        DOF(3).Axis5DIR = Normal

        DOF(4).Type = DOFType.Heave
        DOF(4).Axis0Percentage = 100
        DOF(4).Axis0DIR = Reverse
        DOF(4).Axis1Percentage = 100
        DOF(4).Axis1DIR = Reverse
        DOF(4).Axis2Percentage = 100
        DOF(4).Axis2DIR = Reverse
        DOF(4).Axis3Percentage = 0
        DOF(4).Axis3DIR = Normal
        DOF(4).Axis4Percentage = 0
        DOF(4).Axis4DIR = Normal
        DOF(4).Axis5Percentage = 0
        DOF(4).Axis5DIR = Normal

        DOF(5).Type = DOFType.Yaw
        DOF(5).Axis0Percentage = 0
        DOF(5).Axis0DIR = Normal
        DOF(5).Axis1Percentage = 0
        DOF(5).Axis1DIR = Normal
        DOF(5).Axis2Percentage = 0
        DOF(5).Axis2DIR = Normal
        DOF(5).Axis3Percentage = 100
        DOF(5).Axis3DIR = Reverse
        DOF(5).Axis4Percentage = 0
        DOF(5).Axis4DIR = Normal
        DOF(5).Axis5Percentage = 0
        DOF(5).Axis5DIR = Normal
    End Sub

    Public Sub setDOF(DOFID As Integer, DOFType As Integer, DOFPercent_0 As Double, DOFDIR_0 As Boolean, DOFPercent_1 As Double, DOFDIR_1 As Boolean, DOFPercent_2 As Double, DOFDIR_2 As Boolean, DOFPercent_3 As Double, DOFDIR_3 As Boolean, DOFPercent_4 As Double, DOFDIR_4 As Boolean, DOFPercent_5 As Double, DOFDIR_5 As Boolean)
        If DOFID >= 0 And DOFID < 6 Then
            DOF(DOFID).Type = DOFType
            DOF(DOFID).Axis0Percentage = DOFPercent_0
            DOF(DOFID).Axis0DIR = DOFDIR_0
            DOF(DOFID).Axis1Percentage = DOFPercent_1
            DOF(DOFID).Axis1DIR = DOFDIR_1
            DOF(DOFID).Axis2Percentage = DOFPercent_2
            DOF(DOFID).Axis2DIR = DOFDIR_2
            DOF(DOFID).Axis3Percentage = DOFPercent_3
            DOF(DOFID).Axis3DIR = DOFDIR_3
            DOF(DOFID).Axis4Percentage = DOFPercent_4
            DOF(DOFID).Axis4DIR = DOFDIR_4
            DOF(DOFID).Axis5Percentage = DOFPercent_5
            DOF(DOFID).Axis5DIR = DOFDIR_5
        End If
    End Sub

    Public Sub useSimtools(Mode As Boolean)
        Simtools = Mode
    End Sub

    Public Sub setUsage(Percentage As Integer)
        If Percentage > 100 Then Percentage = 100
        If Percentage < 0 Then Percentage = 0
        MotionUsage = Percentage * 0.01
    End Sub

    Function getUsage() As Integer
        Return Convert.ToInt16(MotionUsage * 100)
    End Function

    Public Function GetAxisOutput(inRoll As Double, inPitch As Double, inYaw As Double, inHeave As Double, inSurge As Double, inSway As Double) As AxisStruct
        Dim AxisType(6) As AxisStruct
        Dim AxisOut As AxisStruct
        Dim AxisDist As Double = 0

        '--- input offset --- (simtool only)
        If Simtools Then
            inRoll = (inRoll - 32768.0) / 327.68
            inPitch = (inPitch - 32768.0) / 327.68
            inYaw = (inYaw - 32768.0) / 327.68
            inHeave = (inHeave - 32768.0) / 327.68
            inSurge = (inSurge - 32768.0) / 327.68
            inSway = (inSway - 32768.0) / 327.68
        End If

        '--- input clamp ---
        If inRoll > AxisMax Then inRoll = AxisMax
        If inRoll < AxisMin Then inRoll = AxisMin
        If inPitch > AxisMax Then inPitch = AxisMax
        If inPitch < AxisMin Then inPitch = AxisMin
        If inYaw > AxisMax Then inYaw = AxisMax
        If inYaw < AxisMin Then inYaw = AxisMin
        If inHeave > AxisMax Then inHeave = AxisMax
        If inHeave < AxisMin Then inHeave = AxisMin
        If inSurge > AxisMax Then inSurge = AxisMax
        If inSurge < AxisMin Then inSurge = AxisMin
        If inSway > AxisMax Then inSway = AxisMax
        If inSway < AxisMin Then inSway = AxisMin

        '--- input scaleing ---
        inRoll *= MotionUsage
        inPitch *= MotionUsage
        inYaw *= MotionUsage
        inHeave *= MotionUsage
        inSurge *= MotionUsage
        inSway *= MotionUsage

        '--- interpolation zoning ---

        'Debug.WriteLine(inSway)


        '--- input interpolation ---
        'RollShadow = lerp(RollShadow, inRoll, TRoll)
        'PitchShadow = lerp(PitchShadow, inPitch, TPitch)
        'YawShadow = lerp(YawShadow, inYaw, TYaw)
        'HeaveShadow = lerp(HeaveShadow, inHeave, THeave)
        'SurgeShadow = lerp(SurgeShadow, inSurge, TSurge)
        'SwayShadow = lerp(SwayShadow, inSway, TSway)
        'inRoll = RollShadow
        'inPitch = PitchShadow
        'inYaw = YawShadow
        'inHeave = HeaveShadow
        'inSurge = SurgeShadow
        'inSway = SwayShadow

        '--- input distant limit ---
        'AxisDist = Math.Abs(AxisShadow.A1 - inRoll)
        'If AxisDist > DistLimit Then inRoll -= DistLimit
        'AxisShadow.A1 = inRoll
        'AxisDist = Math.Abs(AxisShadow.A2 - inPitch)
        'If AxisDist > DistLimit Then inPitch -= DistLimit
        'AxisShadow.A2 = inPitch
        'AxisDist = Math.Abs(AxisShadow.A3 - inYaw)
        'If AxisDist > DistLimit Then inYaw -= DistLimit
        'AxisShadow.A3 = inYaw
        'AxisDist = Math.Abs(AxisShadow.A4 - inHeave)
        'If AxisDist > DistLimit Then inHeave -= DistLimit
        'AxisShadow.A4 = inHeave
        'AxisDist = Math.Abs(AxisShadow.A5 - inSurge)
        'If AxisDist > DistLimit Then inSurge -= DistLimit
        'AxisShadow.A5 = inSurge
        'AxisDist = Math.Abs(AxisShadow.A6 - inSway)
        'If AxisDist > DistLimit Then inSway -= DistLimit
        'AxisShadow.A6 = inSway

        '--- axis calculation ---
        For i As Integer = 0 To 5
            AxisType(i).A0 = 0.0
            AxisType(i).A1 = 0.0
            AxisType(i).A2 = 0.0
            AxisType(i).A3 = 0.0
            AxisType(i).A4 = 0.0
            AxisType(i).A5 = 0.0
            If DOF(i).Type > 0 Then
                Select Case DOF(i).Type
                    Case DOFType.Roll

                        If Not DOF(i).Axis0Percentage = 0 Then
                            AxisType(i).A0 = inRoll * (DOF(i).Axis0Percentage * 0.01)
                            If DOF(i).Axis0DIR Then AxisType(i).A0 *= -1
                        End If
                        If Not DOF(i).Axis1Percentage = 0 Then
                            AxisType(i).A1 = inRoll * (DOF(i).Axis1Percentage * 0.01)
                            If DOF(i).Axis1DIR Then AxisType(i).A1 *= -1
                        End If
                        If Not DOF(i).Axis2Percentage = 0 Then
                            AxisType(i).A2 = inRoll * (DOF(i).Axis2Percentage * 0.01)
                            If DOF(i).Axis2DIR Then AxisType(i).A2 *= -1
                        End If
                        If Not DOF(i).Axis3Percentage = 0 Then
                            AxisType(i).A3 = inRoll * (DOF(i).Axis3Percentage * 0.01)
                            If DOF(i).Axis3DIR Then AxisType(i).A3 *= -1
                        End If
                        If Not DOF(i).Axis4Percentage = 0 Then
                            AxisType(i).A4 = inRoll * (DOF(i).Axis4Percentage * 0.01)
                            If DOF(i).Axis4DIR Then AxisType(i).A4 *= -1
                        End If
                        If Not DOF(i).Axis5Percentage = 0 Then
                            AxisType(i).A5 = inRoll * (DOF(i).Axis5Percentage * 0.01)
                            If DOF(i).Axis5DIR Then AxisType(i).A5 *= -1
                        End If

                    Case DOFType.Pitch
                        If Not DOF(i).Axis0Percentage = 0 Then
                            AxisType(i).A0 = inPitch * (DOF(i).Axis0Percentage * 0.01)
                            If DOF(i).Axis0DIR Then AxisType(i).A0 *= -1
                        End If
                        If Not DOF(i).Axis1Percentage = 0 Then
                            AxisType(i).A1 = inPitch * (DOF(i).Axis1Percentage * 0.01)
                            If DOF(i).Axis1DIR Then AxisType(i).A1 *= -1
                        End If
                        If Not DOF(i).Axis2Percentage = 0 Then
                            AxisType(i).A2 = inPitch * (DOF(i).Axis2Percentage * 0.01)
                            If DOF(i).Axis2DIR Then AxisType(i).A2 *= -1
                        End If
                        If Not DOF(i).Axis3Percentage = 0 Then
                            AxisType(i).A3 = inPitch * (DOF(i).Axis3Percentage * 0.01)
                            If DOF(i).Axis3DIR Then AxisType(i).A3 *= -1
                        End If
                        If Not DOF(i).Axis4Percentage = 0 Then
                            AxisType(i).A4 = inPitch * (DOF(i).Axis4Percentage * 0.01)
                            If DOF(i).Axis4DIR Then AxisType(i).A4 *= -1
                        End If
                        If Not DOF(i).Axis5Percentage = 0 Then
                            AxisType(i).A5 = inPitch * (DOF(i).Axis5Percentage * 0.01)
                            If DOF(i).Axis5DIR Then AxisType(i).A5 *= -1
                        End If
                    Case DOFType.Yaw
                        If Not DOF(i).Axis0Percentage = 0 Then
                            AxisType(i).A0 = inYaw * (DOF(i).Axis0Percentage * 0.01)
                            If DOF(i).Axis0DIR Then AxisType(i).A0 *= -1
                        End If
                        If Not DOF(i).Axis1Percentage = 0 Then
                            AxisType(i).A1 = inYaw * (DOF(i).Axis1Percentage * 0.01)
                            If DOF(i).Axis1DIR Then AxisType(i).A1 *= -1
                        End If
                        If Not DOF(i).Axis2Percentage = 0 Then
                            AxisType(i).A2 = inYaw * (DOF(i).Axis2Percentage * 0.01)
                            If DOF(i).Axis2DIR Then AxisType(i).A2 *= -1
                        End If
                        If Not DOF(i).Axis3Percentage = 0 Then
                            AxisType(i).A3 = inYaw * (DOF(i).Axis3Percentage * 0.01)
                            If DOF(i).Axis3DIR Then AxisType(i).A3 *= -1
                        End If
                        If Not DOF(i).Axis4Percentage = 0 Then
                            AxisType(i).A4 = inYaw * (DOF(i).Axis4Percentage * 0.01)
                            If DOF(i).Axis4DIR Then AxisType(i).A4 *= -1
                        End If
                        If Not DOF(i).Axis5Percentage = 0 Then
                            AxisType(i).A5 = inYaw * (DOF(i).Axis5Percentage * 0.01)
                            If DOF(i).Axis5DIR Then AxisType(i).A5 *= -1
                        End If
                    Case DOFType.Heave
                        If Not DOF(i).Axis0Percentage = 0 Then
                            AxisType(i).A0 = inHeave * (DOF(i).Axis0Percentage * 0.01)
                            If DOF(i).Axis0DIR Then AxisType(i).A0 *= -1
                        End If
                        If Not DOF(i).Axis1Percentage = 0 Then
                            AxisType(i).A1 = inHeave * (DOF(i).Axis1Percentage * 0.01)
                            If DOF(i).Axis1DIR Then AxisType(i).A1 *= -1
                        End If
                        If Not DOF(i).Axis2Percentage = 0 Then
                            AxisType(i).A2 = inHeave * (DOF(i).Axis2Percentage * 0.01)
                            If DOF(i).Axis2DIR Then AxisType(i).A2 *= -1
                        End If
                        If Not DOF(i).Axis3Percentage = 0 Then
                            AxisType(i).A3 = inHeave * (DOF(i).Axis3Percentage * 0.01)
                            If DOF(i).Axis3DIR Then AxisType(i).A3 *= -1
                        End If
                        If Not DOF(i).Axis4Percentage = 0 Then
                            AxisType(i).A4 = inHeave * (DOF(i).Axis4Percentage * 0.01)
                            If DOF(i).Axis4DIR Then AxisType(i).A4 *= -1
                        End If
                        If Not DOF(i).Axis5Percentage = 0 Then
                            AxisType(i).A5 = inHeave * (DOF(i).Axis5Percentage * 0.01)
                            If DOF(i).Axis5DIR Then AxisType(i).A5 *= -1
                        End If
                    Case DOFType.Surge
                        If Not DOF(i).Axis0Percentage = 0 Then
                            AxisType(i).A0 = inSurge * (DOF(i).Axis0Percentage * 0.01)
                            If DOF(i).Axis0DIR Then AxisType(i).A0 *= -1
                        End If
                        If Not DOF(i).Axis1Percentage = 0 Then
                            AxisType(i).A1 = inSurge * (DOF(i).Axis1Percentage * 0.01)
                            If DOF(i).Axis1DIR Then AxisType(i).A1 *= -1
                        End If
                        If Not DOF(i).Axis2Percentage = 0 Then
                            AxisType(i).A2 = inSurge * (DOF(i).Axis2Percentage * 0.01)
                            If DOF(i).Axis2DIR Then AxisType(i).A2 *= -1
                        End If
                        If Not DOF(i).Axis3Percentage = 0 Then
                            AxisType(i).A3 = inSurge * (DOF(i).Axis3Percentage * 0.01)
                            If DOF(i).Axis3DIR Then AxisType(i).A3 *= -1
                        End If
                        If Not DOF(i).Axis4Percentage = 0 Then
                            AxisType(i).A4 = inSurge * (DOF(i).Axis4Percentage * 0.01)
                            If DOF(i).Axis4DIR Then AxisType(i).A4 *= -1
                        End If
                        If Not DOF(i).Axis5Percentage = 0 Then
                            AxisType(i).A5 = inSurge * (DOF(i).Axis5Percentage * 0.01)
                            If DOF(i).Axis5DIR Then AxisType(i).A5 *= -1
                        End If
                    Case DOFType.sway

                        If Not DOF(i).Axis0Percentage = 0 Then
                            AxisType(i).A0 = inSway * (DOF(i).Axis0Percentage * 0.01)
                            If DOF(i).Axis0DIR Then AxisType(i).A0 *= -1
                        End If
                        If Not DOF(i).Axis1Percentage = 0 Then
                            AxisType(i).A1 = inSway * (DOF(i).Axis1Percentage * 0.01)
                            If DOF(i).Axis1DIR Then AxisType(i).A1 *= -1
                        End If
                        If Not DOF(i).Axis2Percentage = 0 Then
                            AxisType(i).A2 = inSway * (DOF(i).Axis2Percentage * 0.01)
                            If DOF(i).Axis2DIR Then AxisType(i).A2 *= -1
                        End If
                        If Not DOF(i).Axis3Percentage = 0 Then
                            AxisType(i).A3 = inSway * (DOF(i).Axis3Percentage * 0.01)
                            If DOF(i).Axis3DIR Then AxisType(i).A3 *= -1
                        End If
                        If Not DOF(i).Axis4Percentage = 0 Then
                            AxisType(i).A4 = inSway * (DOF(i).Axis4Percentage * 0.01)
                            If DOF(i).Axis4DIR Then AxisType(i).A4 *= -1
                        End If
                        If Not DOF(i).Axis5Percentage = 0 Then
                            AxisType(i).A5 = inSway * (DOF(i).Axis5Percentage * 0.01)
                            If DOF(i).Axis5DIR Then AxisType(i).A5 *= -1
                        End If
                End Select
            End If
        Next

        AxisOut.A0 = ((AxisType(0).A0 + AxisType(1).A0 + AxisType(2).A0 + AxisType(3).A0 + AxisType(4).A0 + AxisType(5).A0)) * AxisUsage.A0
        AxisOut.A1 = ((AxisType(0).A1 + AxisType(1).A1 + AxisType(2).A1 + AxisType(3).A1 + AxisType(4).A1 + AxisType(5).A1)) * AxisUsage.A1
        AxisOut.A2 = ((AxisType(0).A2 + AxisType(1).A2 + AxisType(2).A2 + AxisType(3).A2 + AxisType(4).A2 + AxisType(5).A2)) * AxisUsage.A2
        AxisOut.A3 = ((AxisType(0).A3 + AxisType(1).A3 + AxisType(2).A3 + AxisType(3).A3 + AxisType(4).A3 + AxisType(5).A3)) * AxisUsage.A3
        AxisOut.A4 = ((AxisType(0).A4 + AxisType(1).A4 + AxisType(2).A4 + AxisType(3).A4 + AxisType(4).A4 + AxisType(5).A4)) * AxisUsage.A4
        AxisOut.A5 = ((AxisType(0).A5 + AxisType(1).A5 + AxisType(2).A5 + AxisType(3).A5 + AxisType(4).A5 + AxisType(5).A5)) * AxisUsage.A5

        '--- output clamp ---
        If AxisOut.A0 > AxisMax Then AxisOut.A0 = AxisMax
        If AxisOut.A0 < AxisMin Then AxisOut.A0 = AxisMin
        If AxisOut.A1 > AxisMax Then AxisOut.A1 = AxisMax
        If AxisOut.A1 < AxisMin Then AxisOut.A1 = AxisMin
        If AxisOut.A2 > AxisMax Then AxisOut.A2 = AxisMax
        If AxisOut.A2 < AxisMin Then AxisOut.A2 = AxisMin
        If AxisOut.A3 > AxisMax Then AxisOut.A3 = AxisMax
        If AxisOut.A3 < AxisMin Then AxisOut.A3 = AxisMin
        If AxisOut.A4 > AxisMax Then AxisOut.A4 = AxisMax
        If AxisOut.A4 < AxisMin Then AxisOut.A4 = AxisMin
        If AxisOut.A5 > AxisMax Then AxisOut.A5 = AxisMax
        If AxisOut.A5 < AxisMin Then AxisOut.A5 = AxisMin

        'AxisOut.A1 = Math.Truncate(OutputScale * AxisOut.A1) + OutputOffset
        'AxisOut.A2 = Math.Truncate(OutputScale * AxisOut.A2) + OutputOffset
        'AxisOut.A3 = Math.Truncate(OutputScale * AxisOut.A3) + OutputOffset
        'AxisOut.A4 = Math.Truncate(OutputScale * AxisOut.A4) + OutputOffset
        'AxisOut.A5 = Math.Truncate(OutputScale * AxisOut.A5) + OutputOffset
        'AxisOut.A6 = Math.Truncate(OutputScale * AxisOut.A6) + OutputOffset

        Return AxisOut
    End Function

    Public Function GetCircleShortPath(Origin As Single, Target As Single) As Single
        Dim RMax As Single = 360.0
        Dim RDiff As Single = 0.0
        Dim MDiff As Single = 0.0
        Dim SDiff As Single = 0.0
        Dim NTemp As Single = 0.0
        If Origin > Target Then
            RDiff = Origin - Target
        Else
            RDiff = Target - Origin
        End If
        '-- fMod function : fMod(nNumber, nDiv) : MDiff = fMod(RDiff, RMax)
        NTemp = RDiff / RMax
        NTemp = (NTemp - Convert.ToInt32(NTemp))
        If NTemp < 1 Then NTemp = NTemp + 1
        MDiff = NTemp * RMax
        '--------
        If MDiff > RMax / 2 Then
            SDiff = RMax - MDiff
            If Target > Origin Then SDiff = SDiff * -1
        Else
            SDiff = MDiff
            If Origin > Target Then SDiff = SDiff * -1
        End If
        Return SDiff
    End Function

    Public Function GetCRC(Axis1 As Integer, Axis2 As Integer, Axis3 As Integer, Axis4 As Integer, Axis5 As Integer, Axis6 As Integer) As Integer
        Dim ResCRC As Integer = 0
        Dim HTemp_A As Integer = 0
        Dim HTemp_B As Integer = 0
        ResCRC = DClamp(Axis1, 5)
        ResCRC += DClamp(Axis2, 5)
        ResCRC += DClamp(Axis3, 5)
        ResCRC += DClamp(Axis4, 5)
        ResCRC += DClamp(((Math.Abs(Axis2) + 1) * (Math.Abs(Axis5) + 1)), 3)
        ResCRC += DClamp(((Math.Abs(Axis3) + 1) * (Math.Abs(Axis4) + 1)), 3)
        Return ResCRC
    End Function

    Private Function DClamp(RawValue As Integer, MaxDigit As Integer) As Integer
        Dim Res As Integer = 0
        Dim MaxD As Integer = MaxDigit
        If MaxD > 8 Then MaxD = 8
        If MaxD < 1 Then MaxD = 1
        MaxD = Convert.ToInt32(Math.Pow(10, MaxD))
        Res = RawValue
        If Res < 0 Then
            MaxD *= -1
            Do Until Res >= MaxD
                Res -= MaxD
            Loop
        Else
            Do Until Res <= MaxD
                Res -= MaxD
            Loop
        End If
        Res += Math.Truncate(Math.Abs(RawValue - MaxD) / MaxD)
        Return Res
    End Function

    Public Function lerp(V0 As Double, V1 As Double, T As Double) As Double
        Return V0 + T * (V1 - V0)
    End Function

    Public Function MoveCap(Origin As Single, Target As Single, Cap As Single) As Single
        Dim Res As Single = Target
        If Math.Abs(Origin - Target) > Cap Then
            Select Case Origin - Target
                Case > 0
                    Res = Origin - Cap
                Case < 0
                    Res = Origin + Cap
            End Select
        End If
        Return Res
    End Function

    Public Function toSingle(StrValue As String) As Single
        Dim Res As Single = 0.0
        Dim REGValue As Single = 0.0
        Dim CharRX As Char
        Dim CHValue As UInt16 = 0
        Dim FracPoint As Single = 0.0
        Dim REGFraction As Single = 0.0
        Dim REGMinus As Boolean = False

        If Not StrValue <> "" Then Return 0.0
        For i As Integer = 0 To StrValue.Length - 1
            CharRX = StrValue.Substring(i, 1)
            CHValue = Convert.ToUInt16(CharRX)
            Select Case CHValue
                Case 45 'Minus symbol found
                    REGMinus = True
                    Exit Select
                Case 46 'Decimal point found
                    FracPoint = 1.0
                    REGFraction = 0.0
                    Exit Select
                Case 48 To 57 'Number handing
                    If FracPoint < 1.0 Then
                        REGValue = (REGValue * 10.0) + (CHValue - 48.0)
                    Else
                        If FracPoint < 1000.0 Then
                            REGFraction = (REGFraction * 10.0) + (CHValue - 48.0)
                            FracPoint = FracPoint * 10.0
                        End If
                    End If
                    Exit Select
                Case Else
                    Exit For
            End Select
        Next

        If FracPoint > 0 Then
            Res = REGValue + (REGFraction / FracPoint)
        Else
            Res = REGValue
        End If
        If REGMinus Then
            Res = Res * -1.0
        End If
        Return Res
    End Function

    Public Function toInt(StrValue As String) As Integer
        Dim Res As Integer = 0
        Dim REGValue As Integer = 0
        Dim CharRX As Char
        Dim CHValue As UInt16 = 0
        Dim REGMinus As Boolean = False

        If Not StrValue <> "" Then Return 0
        For i As Integer = 0 To StrValue.Length - 1
            CharRX = StrValue.Substring(i, 1)
            CHValue = Convert.ToUInt16(CharRX)
            Select Case CHValue
                Case 45 'Minus symbol found
                    REGMinus = True
                    Exit Select
                Case 48 To 57 'Number handing
                    REGValue = (REGValue * 10.0) + (CHValue - 48.0)
                    Exit Select
                Case Else
                    Exit For
            End Select
        Next
        Res = REGValue
        If REGMinus Then
            Res = Res * -1.0
        End If
        Return Res
    End Function
End Module


