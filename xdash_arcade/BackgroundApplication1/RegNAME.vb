Module Reg

    Structure CustomIPBinding
        Dim commandRXPort As Integer
        Dim broadcastRXPort As Integer
        Dim IPBinding_A As System.Net.IPAddress
        Dim IPBinding_B As System.Net.IPAddress
        Dim LocalIP As System.Net.IPAddress
        Dim AUXData_MotorCount As Integer
    End Structure

    Structure CustomController
        Dim WheelPOS As UInt16
        Dim OprState As Byte
        Dim AnalogCH_1 As Byte
        Dim AnalogCH_2 As Byte
        Dim AnalogCH_3 As Byte
        Dim AnalogCH_4 As Byte
        Dim AnalogCH_5 As Byte
        Dim AnalogCH_6 As Byte
        Dim AnalogCH_7 As Byte
        Dim AnalogCH_8 As Byte
        Dim Button As UInt16
    End Structure

    Structure CustomDriveError
        Dim General As UInt16
        Dim Motor As UInt16
        Dim Encoder As UInt16
    End Structure

    '----- Register table name referance ----
    Public Enum Register As Integer
        'Axis0_ManualPOS = 20
        'Axis1_ManualPOS = 21
        'Axis2_ManualPOS = 22
        'Axis3_ManualPOS = 23

        Coin_Counter = 20
        Coin_Shadow = 21
        Service_Counter = 22
        Service_Shadow = 23
        IO_State = 24
        IO_Shadow = 25

        Output_Percentage = 40  'motor output scale (0 - 100) --!-- this override by soft start/stop function

        Axis0_Offset = 50      '<-- in PPR scale --
        Axis1_Offset = 51      '<-- in PPR scale --
        Axis2_Offset = 52      '<-- in PPR scale --
        Axis3_Offset = 53      '<-- in PPR scale --
        Axis4_Offset = 54      '<-- in PPR scale --
        Axis5_Offset = 55      '<-- in PPR scale --
        Axis6_Offset = 56      '<-- in PPR scale --
        Axis7_Offset = 57      '<-- in PPR scale --
        'Axis0_GearCenter = 58  '<-- in Degree --
        'Axis1_GearCenter = 59  '<-- in Degree --
        'Axis2_GearCenter = 60  '<-- in Degree --
        'Axis3_GearCenter = 61  '<-- in Degree --
        'Axis4_GearCenter = 62  '<-- in Degree --
        'Axis5_GearCenter = 63  '<-- in Degree --
        'Axis6_GearCenter = 64  '<-- in Degree --
        'Axis7_GearCenter = 65  '<-- in Degree --

        Setting_Update = 66    '<-- reload config request

        'Axis0_ManualMove = 70
        'Axis1_ManualMove = 71
        'Axis2_ManualMove = 72
        'Axis3_ManualMove = 73
        'Axis4_ManualMove = 74
        'Axis5_ManualMove = 75
        'Axis6_ManualMove = 76
        'Axis7_ManualMove = 77



        Axis0_ExtEncoder = 88 'EXT-IO Ref REG-X
        Axis1_ExtEncoder = 89 'EXT-IO Ref REG-Y
        Axis2_ExtEncoder = 90 'EXT-IO Ref REG-Z
        Axis3_ExtEncoder = 65 'EXT-IO Ref REG-A
        Axis4_ExtEncoder = 66 'EXT-IO Ref REG-B
        Axis5_ExtEncoder = 67 'EXT-IO Ref REG-C

        ' --- Realtime counter for motor time/distant record
        Axis0_TotalRev = 91
        Axis1_TotalRev = 92
        Axis2_TotalRev = 93
        Axis3_TotalRev = 94
        Axis0_TotalTime = 95
        Axis1_TotalTime = 96
        Axis2_TotalTime = 97
        Axis3_TotalTime = 98

        MotorState_Request = 100
        MotorControl_Busy = 101
        MotorState_Current = 102
        MotorState_Parking = 103

        Wheel_Velocity = 110
        Wheel_Velocity_Shadow = 111
        Wheel_LED = 112
        Wheel_LED_Shadow = 113

    End Enum

    Public Enum GPIORespond As Integer

        ' --- Absolute Encoder and HomeSensor use same io port but have separate read command
        '     when read data will reflect in both register and only valid for correspond request

        AbsoluteEncoder_Axis0 = 88
        AbsoluteEncoder_Axis1 = 89
        AbsoluteEncoder_Axis2 = 90
        AbsoluteEncoder_Axis3 = 65

        AnalogInput0 = 85
        AnalogInput1 = 86
        AnalogInput2 = 87
        AnalogInput3 = 66

        I2C_EXCOM_BUFFER0_Status = 82
        I2C_EXCOM_BUFFER1_Status = 83
        I2C_EXCOM_BUFFER2_Status = 84
        I2C_EXCOM_BUFFER3_Status = 67

        HomeSensor0_Status = 72
        HomeSensor1_Status = 72
        HomeSensor2_Status = 72
        HomeSensor3_Status = 72

        ' --- ODrive not support request id , all respond go in same register
        ALL_ODrive_Respond = 64

    End Enum

    Public Enum State As Integer
        Idle = 0
        Stop_Inprogress = 1
        Stop_Complete = 2
        Run_Inprogress = 3
        Run_Complete = 4
    End Enum

    Public Enum MachineEventID As Integer
        ServiceEvent = 0
        CoinEvent = 1
        GPIOEvent = 2
        MotorEvent = 3
    End Enum

    Public Enum RespondTo As Integer
        PC = 0
        Dashboard = 1
    End Enum

    Public Const ColdStart = 0
    Public Const Game_Controller_Type = 80 '0:ODrive(Classic way) 1:Raw_USB-HID  2:Wheel_USB-HID
    'Public Const ScriptRequest = 100

    'after set to 1 all pos input must set to 0 or machine can move in unpredictable
    'it safe to turn motor off befor apply setting
    '0,400 = not set 401 = use current pos 402 = apply success
    'Public Const SetAsOffset = 62
    '---
    Public Const OFFSET_NONE = 400
    Public Const OFFSET_USE = 401
    Public Const OFFSET_APPLY = 402

    'Public Const Axis0_HomeDir = 70
    'Public Const Axis1_HomeDir = 71

    'Public Const MOTOR_CONTROL = 100
    'Public Const MANUAL_CONTROL = 99
    'Public Const CONTROL_BUSY = 101


    Public Const Device_ODrive0 = 0
    Public Const Device_ODrive1 = 1
    Public Const Device_ODrive2 = 2
    Public Const Device_ODrive3 = 3
    Public Const Device_SystemIO = 4
    Public Const Device_Display = 5
    Public Const Device_Wheel = 6

    Public Const IO_NULL = 0
    Public Const IO_SPACE = 32
    Public Const IO_HEAD = 64 '[@] start marker
    Public Const IO_A = 65
    Public Const IO_B = 66
    Public Const IO_C = 67
    Public Const IO_D = 68
    Public Const IO_E = 69
    Public Const IO_F = 70
    Public Const IO_G = 71
    Public Const IO_H = 72
    Public Const IO_I = 73
    Public Const IO_J = 74
    Public Const IO_K = 75
    Public Const IO_L = 76
    Public Const IO_M = 77
    Public Const IO_N = 78
    Public Const IO_O = 79
    Public Const IO_P = 80
    Public Const IO_Q = 81
    Public Const IO_R = 82
    Public Const IO_S = 83
    Public Const IO_T = 84
    Public Const IO_U = 85
    Public Const IO_V = 86
    Public Const IO_W = 87
    Public Const IO_X = 88
    Public Const IO_Y = 89
    Public Const IO_Z = 90

    Public Function ControlPermission(RegisterID As Integer) As Boolean
        Dim res As Boolean
        Select Case RegisterID
            Case Register.Setting_Update : res = True
            Case Register.MotorState_Request : res = True
            Case Register.MotorState_Current : res = False
            Case Register.MotorControl_Busy : res = False

            Case Register.Axis0_Offset : res = False
            Case Register.Axis1_Offset : res = False
            Case Register.Axis2_Offset : res = False
            Case Register.Axis3_Offset : res = False
            Case Register.Axis4_Offset : res = False
            Case Register.Axis5_Offset : res = False
            Case Register.Axis6_Offset : res = False
            Case Register.Axis7_Offset : res = False

            Case Register.Axis0_TotalRev : res = False
            Case Register.Axis1_TotalRev : res = False
            Case Register.Axis2_TotalRev : res = False
            Case Register.Axis3_TotalRev : res = False

            Case Register.Axis0_TotalTime : res = False
            Case Register.Axis1_TotalTime : res = False
            Case Register.Axis2_TotalTime : res = False
            Case Register.Axis3_TotalTime : res = False

            Case Register.Wheel_Velocity : res = True
            Case Register.Wheel_LED : res = True

            Case IO_NULL : res = False
            Case IO_SPACE : res = False
            Case IO_HEAD : res = False
            Case IO_A : res = False
            Case IO_B : res = False
            Case IO_C : res = False
            Case IO_D : res = False
            Case IO_E : res = False
            Case IO_F : res = False
            Case IO_G : res = False
            Case IO_H : res = False
            Case IO_I : res = False
            Case IO_J : res = False
            Case IO_K : res = False
            Case IO_L : res = False
            Case IO_M : res = False
            Case IO_N : res = False
            Case IO_O : res = False
            Case IO_P : res = False
            Case IO_Q : res = False
            Case IO_R : res = False
            Case IO_S : res = False
            Case IO_T : res = False
            Case IO_U : res = False
            Case IO_V : res = False
            Case IO_W : res = False
            Case IO_X : res = False
            Case IO_Y : res = False
            Case IO_Z : res = False
            Case Else
                res = False
        End Select
        Return res
    End Function

End Module
