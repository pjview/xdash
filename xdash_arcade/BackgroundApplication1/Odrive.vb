Module ODrive

    Structure MotorParameter
        Dim Current_Limit As Single
        Dim Current_Limit_Tolerance As Single
        Dim Current_Range As Single
        Dim Velocity_Limit As Single
        Dim Trajectory_Velocity_Limit As Single
        Dim Acceleration_Limit As Single
        Dim Deacceleration_Limit As Single
        Dim Position_Gain As Single
        Dim Velocity_Gain As Single
        Dim Velocity_Integrator_Gain As Single
        Dim Current_Control_Bandwidth As Single
        Dim Calibration_Current As Single
        Dim Calibration_Velocity As Single
        Dim Calibration_Acceleration As Single
        Dim Calibration_Ramp As Single
        Dim Safe_Velocity As Single
        Dim ConfigOK As Boolean
    End Structure

    Structure MotorStatus
        Dim Text As String
        Dim ErrText As String
        Dim Enable As Boolean
        Dim Ready As Boolean
        Dim LimitOverride As Boolean
        Dim Current As Single
        Dim Pos As Single
    End Structure

    Public Structure AxisParameter
        Dim Type As Integer
        Dim IndexEnable As Boolean
        Dim HomeEnable As Boolean
        Dim EndstopEnable As Boolean
        Dim EndstopOffset As Integer
        Dim HomeDeg As Single
        Dim HomeDir As Boolean
        Dim HomeEnc As Integer
        Dim HomeLimit As Integer
        Dim HomeTotalance As Single
        Dim HomeTimeout As Integer
        Dim IndexTimeout As Integer
        Dim PPR As Integer
        Dim RangePositive As Single
        Dim RangeNegative As Single
        Dim GearRatio As Single
        Dim CalibrationProfile As Integer
        Dim RunProfile As Integer
        Dim ParkProfile As Integer
        Dim ConfigOK As Boolean
    End Structure

    Public Enum AxisType As Integer
        None = 0
        Circular = 1
        Linear = 2
        Direct = 3
    End Enum

    Public Enum Direction
        Normal = False
        Reverse = True
    End Enum

    Public Enum Unit As Integer
        Pulse = 0
        Turn = 1
    End Enum

    'Config
    Public Const BRAKE_RESISTANCE As UInt16 = 100
    Public Const ENABLE_BRAKE_RESISTOR As UInt16 = 109
    Public Const DC_MAX_NEGATIVE_CURRENT As UInt16 = 108
    Public Const ENABLE_UART As UInt16 = 900
    Public Const ENABLE_ASCII_ON_USB As UInt16 = 901
    Public Const DC_BUS_UNDERVOLTAGE_TRIP As UInt16 = 902
    Public Const DC_BUS_OVERVOLTAGE_TRIP As UInt16 = 903
    Public Const POLE_PAIRS As UInt16 = 102
    Public Const CALIBRATION_CURRENT As UInt16 = 103
    Public Const ENCODER_CPR As UInt16 = 104
    Public Const ENCODER_MODE As UInt16 = 105
    Public Const ENCODER_USE_INDEX As UInt16 = 106
    Public Const PM_FLUX_LINKAGE As UInt16 = 107

    Public Const ENCODER_OFFSET As UInt16 = 110
    Public Const ENCODER_OFFSET_FLOAT As UInt16 = 904
    Public Const CONTROL_MODE As UInt16 = 111
    Public Const MOTOR_PRE_CALIBRATED As UInt16 = 120
    Public Const ENCODER_PRE_CALIBRATED As UInt16 = 121
    Public Const CURRENT_LIM As UInt16 = 122
    Public Const VEL_LIMIT As UInt16 = 123
    Public Const POS_GAIN As UInt16 = 124
    Public Const VEL_GAIN As UInt16 = 125
    Public Const VEL_INTEGRATOR_GAIN As UInt16 = 126
    Public Const TRAJ_VEL_LIMIT As UInt16 = 127
    Public Const TRAJ_ACCEL_LIMIT As UInt16 = 128
    Public Const TRAJ_DECEL_LIMIT As UInt16 = 129

    'motor.config
    Public Const MOTOR_TYPE As UInt16 = 101

    Public Const RESISTANCE_CALIB_MAX_VOLTAGE As UInt16 = 140
    Public Const REQUESTED_CURRENT_RANGE As UInt16 = 141
    Public Const CURRENT_CONTROL_BANDWIDTH As UInt16 = 142
    Public Const CURRENT_LIMIT_TOLERANCE As UInt16 = 143
    Public Const PHASE_INDUCTANCE As UInt16 = 144
    Public Const PHASE_RESISTANCE As UInt16 = 145
    Public Const CALIBRATION_VELOCITY As UInt16 = 146
    Public Const CALIBRATION_ACCEL As UInt16 = 147
    Public Const CALIBRATION_RAMP As UInt16 = 148

    'Misc variable
    Public Const GENERAL_ERROR As UInt16 = 200
    Public Const MOTOR_ERROR As UInt16 = 201
    Public Const ENCODER_ERROR As UInt16 = 202
    Public Const MOTOR_IS_CALIBRATED As UInt16 = 203
    Public Const ENCODER_IS_READY As UInt16 = 204
    Public Const ENCODER_POS_ESTIMATE As UInt16 = 205
    'odrv0.axis0.encoder.pos_estimate

    'Misc command
    Public Const CURRENT_STATE As UInt16 = 301
    Public Const REQUEST_STATE As UInt16 = 302
    Public Const VBUS_VOLTAGE As UInt16 = 303
    Public Const MEASURED_CURRENT As UInt16 = 304
    Public Const COMMAND_CURRENT As UInt16 = 305
    Public Const FET_TEMPERATURE As UInt16 = 306

    'ASCII command
    Public Const CONTROL_REBOOT As String = "sb" 'after reboot must restart RXTask 
    Public Const SAVE_CONFIG As String = "ss"    'WARNING wait 5 sec for EEROM write to complete or data corruption may occure
    Public Const ERASE_CONFIG As String = "es"   'WARNING this command "DELETE ALL CONFIG" in target ODrive

    'Drive state
    Public Const AXIS_STATE_UNDEFINED As UInt16 = 0                  '<! will fall through To idle
    Public Const AXIS_STATE_IDLE As UInt16 = 1                       '<! disable PWM And Do Nothing
    Public Const AXIS_STATE_STARTUP_SEQUENCE As UInt16 = 2           '<! the actual sequence Is defined by the config.startup_... flags
    Public Const AXIS_STATE_FULL_CALIBRATION_SEQUENCE As UInt16 = 3  '<! run all calibration procedures, Then idle
    Public Const AXIS_STATE_MOTOR_CALIBRATION As UInt16 = 4          '<! run motor calibration
    Public Const AXIS_STATE_SENSORLESS_CONTROL As UInt16 = 5         '<! run sensorless control
    Public Const AXIS_STATE_ENCODER_INDEX_SEARCH As UInt16 = 6       '<! run encoder index search
    Public Const AXIS_STATE_ENCODER_OFFSET_CALIBRATION As UInt16 = 7 '<! run encoder offset calibration
    Public Const AXIS_STATE_CLOSED_LOOP_CONTROL As UInt16 = 8        '<! run closed Loop control

    'Control Mode
    Public Const CTRL_MODE_VOLTAGE_CONTROL As UInt16 = 0
    Public Const CTRL_MODE_CURRENT_CONTROL As UInt16 = 1
    Public Const CTRL_MODE_VELOCITY_CONTROL As UInt16 = 2
    Public Const CTRL_MODE_POSITION_CONTROL As UInt16 = 3
    Public Const CTRL_MODE_TRAJECTORY_CONTROL As UInt16 = 4

    Private Const CTRL_MODE_UNDEFINED_NAME As String = "- NONE -"
    Private Const CTRL_MODE_VOLTAGE_NAME As String = "VOLTAGE"
    Private Const CTRL_MODE_CURRENT_NAME As String = "CURRENT"
    Private Const CTRL_MODE_VELOCITY_NAME As String = "VELOCITY"
    Private Const CTRL_MODE_POSITION_NAME As String = "POSITION"
    Private Const CTRL_MODE_TRAJECTORY_NAME As String = "TRAJECTORY"

    Public Const ENCODER_MODE_INCREMENTAL As UInt16 = 0
    Public Const ENCODER_MODE_HALL As UInt16 = 1
    Public Const ENCODER_MODE_ABSOLUTE As UInt16 = 999

    Private Const ENCODER_MODE_UNDEFINED_NAME As String = "- NONE -"
    Private Const ENCODER_MODE_INCREMENTAL_NAME As String = "INCREMENTAL"
    Private Const ENCODER_MODE_HALL_NAME As String = "HALL"
    Private Const ENCODER_MODE_ABSOLUTE_NAME As String = "ABSOLUTE"

    Public MotorPreset(20) As MotorParameter
    'Public SlowPreset(10) As MotorParameter
    Public Axis(10) As AxisParameter
    Public Motor(10) As MotorStatus

    'Public MotorProfile(10) As Integer
    Public Map_DRV(10) As Integer
    Public Map_OUT(10) As Integer
    Public Map_SW(10) As Integer

    Public Const PositionUnit As Integer = Unit.Turn

    'Public STATE_Wait As Integer = 250
    'Public CMD_Wait As Integer = 50

    Public TCmd() As String = {"t 0 ", "t 1 "}

    'Private StrBuilder As New System.Text.StringBuilder
    Private CMDArray(1000) As Boolean

    Public Sub init()
        System.Array.Clear(CMDArray, 0, CMDArray.Length)
        CMDArray(BRAKE_RESISTANCE) = True
        CMDArray(ENABLE_UART) = True
        CMDArray(ENABLE_ASCII_ON_USB) = True
        CMDArray(DC_BUS_UNDERVOLTAGE_TRIP) = True
        CMDArray(DC_BUS_OVERVOLTAGE_TRIP) = True
        CMDArray(POLE_PAIRS) = True
        CMDArray(CALIBRATION_CURRENT) = True
        CMDArray(ENCODER_CPR) = True
        CMDArray(ENCODER_MODE) = True
        CMDArray(ENCODER_USE_INDEX) = True
        CMDArray(PM_FLUX_LINKAGE) = True
        CMDArray(ENCODER_OFFSET) = True
        CMDArray(ENCODER_OFFSET_FLOAT) = True
        CMDArray(CONTROL_MODE) = True
        CMDArray(MOTOR_PRE_CALIBRATED) = True
        CMDArray(ENCODER_PRE_CALIBRATED) = True
        CMDArray(CURRENT_LIM) = True
        CMDArray(VEL_LIMIT) = True
        CMDArray(POS_GAIN) = True
        CMDArray(VEL_GAIN) = True
        CMDArray(VEL_INTEGRATOR_GAIN) = True
        CMDArray(TRAJ_VEL_LIMIT) = True
        CMDArray(TRAJ_ACCEL_LIMIT) = True
        CMDArray(TRAJ_DECEL_LIMIT) = True
        CMDArray(MOTOR_TYPE) = True
        CMDArray(RESISTANCE_CALIB_MAX_VOLTAGE) = True
        CMDArray(REQUESTED_CURRENT_RANGE) = True
        CMDArray(CURRENT_CONTROL_BANDWIDTH) = True
        CMDArray(CURRENT_LIMIT_TOLERANCE) = True
        CMDArray(PHASE_RESISTANCE) = True
        CMDArray(PHASE_INDUCTANCE) = True
        CMDArray(CALIBRATION_VELOCITY) = True
        CMDArray(CALIBRATION_ACCEL) = True
        CMDArray(CALIBRATION_RAMP) = True
        CMDArray(GENERAL_ERROR) = True
        CMDArray(MOTOR_ERROR) = True
        CMDArray(ENCODER_ERROR) = True
        CMDArray(MOTOR_IS_CALIBRATED) = True
        CMDArray(ENCODER_IS_READY) = True
        CMDArray(CURRENT_STATE) = True
        CMDArray(REQUEST_STATE) = True
        CMDArray(VBUS_VOLTAGE) = True
        CMDArray(MEASURED_CURRENT) = True
        CMDArray(COMMAND_CURRENT) = True
        CMDArray(FET_TEMPERATURE) = True
    End Sub

    Public Function CMDExist(CMDID As Integer) As Boolean
        Return CMDArray(CMDID)
    End Function

    Public Function CommandString(CMDSID As Integer, AxisID As Integer) As String
        Dim CMDS As String = ""
        Select Case CMDSID
            Case = BRAKE_RESISTANCE
                CMDS = "config.brake_resistance"
            Case = MOTOR_TYPE
                CMDS = "axis*.motor.config.motor_type"
            Case = POLE_PAIRS
                CMDS = "axis*.motor.config.pole_pairs"
            Case = CALIBRATION_CURRENT
                CMDS = "axis*.motor.config.calibration_current"
            Case = ENCODER_CPR
                CMDS = "axis*.encoder.config.cpr"
            Case = ENCODER_MODE
                CMDS = "axis*.encoder.config.mode"
            Case = ENCODER_USE_INDEX
                CMDS = "axis*.encoder.config.use_index"
            Case = PM_FLUX_LINKAGE
                CMDS = "axis*.sensorless_estimator.config.pm_flux_linkage"
            Case = ENCODER_OFFSET
                CMDS = "axis*.encoder.config.offset"
            Case = MOTOR_PRE_CALIBRATED
                CMDS = "axis*.motor.config.pre_calibrated"
            Case = ENCODER_PRE_CALIBRATED
                CMDS = "axis*.encoder.config.pre_calibrated"
            Case = CURRENT_LIM
                CMDS = "axis*.motor.config.current_lim"
            Case = VEL_LIMIT
                CMDS = "axis*.controller.config.vel_limit"
            Case = POS_GAIN
                CMDS = "axis*.controller.config.pos_gain"
            Case = VEL_GAIN
                CMDS = "axis*.controller.config.vel_gain"
            Case = VEL_INTEGRATOR_GAIN
                CMDS = "axis*.controller.config.vel_integrator_gain"
            Case = CONTROL_MODE
                CMDS = "axis*.controller.config.control_mode"
            Case = MEASURED_CURRENT
                CMDS = "axis*.motor.current_control.Iq_measured"
            Case = COMMAND_CURRENT
                CMDS = "axis*.motor.current_control.Iq_setpoint"
            Case = FET_TEMPERATURE
                CMDS = "axis*.motor.fet_thermistor.temperature"

            Case = TRAJ_VEL_LIMIT
                CMDS = "axis*.trap_traj.config.vel_limit"
            Case = TRAJ_ACCEL_LIMIT
                CMDS = "axis*.trap_traj.config.accel_limit"
            Case = TRAJ_DECEL_LIMIT
                CMDS = "axis*.trap_traj.config.decel_limit"

            Case = RESISTANCE_CALIB_MAX_VOLTAGE
                CMDS = "axis*.motor.config.resistance_calib_max_voltage"
            Case = REQUESTED_CURRENT_RANGE
                CMDS = "axis*.motor.config.requested_current_range"
            Case = CURRENT_CONTROL_BANDWIDTH
                CMDS = "axis*.motor.config.current_control_bandwidth"
            Case = CURRENT_LIMIT_TOLERANCE
                CMDS = "axis*.motor.config.current_lim_tolerance"
            Case = DC_MAX_NEGATIVE_CURRENT
                CMDS = "config.dc_max_negative_current"
            Case = ENABLE_BRAKE_RESISTOR
                CMDS = "config.enable_brake_resistor"
            Case = PHASE_INDUCTANCE
                CMDS = "axis*.motor.config.phase_inductance"
            Case = PHASE_RESISTANCE
                CMDS = "axis*.motor.config.phase_resistance"
            Case = CALIBRATION_VELOCITY
                CMDS = "axis*.config.calibration_lockin.vel"
            Case = CALIBRATION_ACCEL
                CMDS = "axis*.config.calibration_lockin.accel"
            Case = CALIBRATION_RAMP
                CMDS = "axis*.config.calibration_lockin.ramp_distance"

            Case = GENERAL_ERROR
                CMDS = "axis*.error"
            Case = MOTOR_ERROR
                CMDS = "axis*.motor.error"
            Case = ENCODER_ERROR
                CMDS = "axis*.encoder.error"
            Case = MOTOR_IS_CALIBRATED
                CMDS = "axis*.motor.is_calibrated"
            Case = ENCODER_IS_READY
                CMDS = "axis*.encoder.is_ready"
            Case = ENCODER_POS_ESTIMATE
                CMDS = "axis*.encoder.pos_estimate"

            Case = CURRENT_STATE
                CMDS = "axis*.current_state"
            Case = REQUEST_STATE
                CMDS = "axis*.requested_state"
            Case = VBUS_VOLTAGE
                CMDS = "vbus_voltage"

        End Select
        Return CMDS.Replace("*", AxisID.ToString)
    End Function

    Public Function GeneralError(ErrID As Integer) As String
        Dim ErrSTRING As String = ""
        Select Case ErrID
            Case = 0
                ErrSTRING = "NONE"
            Case = 1
                ErrSTRING = "INVALID_STATE" '<! an invalid state was requested
            Case = 2
                ErrSTRING = "DC_BUS_UNDER_VOLTAGE"
            Case = 4
                ErrSTRING = "DC_BUS_OVER_VOLTAGE"
            Case = 8
                ErrSTRING = "CURRENT_MEASUREMENT_TIMEOUT"
            Case = 16
                ErrSTRING = "BRAKE_RESISTOR_DISARMED" '<! the brake resistor was disconnect
            Case = 32
                ErrSTRING = "MOTOR_DISARMED" '<! the motor was disconnect
            Case = 64
                ErrSTRING = "MOTOR_FAILED" 'check motor error value
            Case = 128
                ErrSTRING = "SENSORLESS_ESTIMATOR_FAILED"
            Case = 256
                ErrSTRING = "ENCODER_FAILED" 'check encoder error value
            Case = 512
                ErrSTRING = "CONTROLLER_FAILED"
            Case = 1024
                ErrSTRING = "POS_CTRL_DURING_SENSORLESS" 'position control cannot use in sensorless mode
            Case = 2048
                ErrSTRING = "WATCHDOG_TIMER_EXPIRED"
            Case Else
                ErrSTRING = "UNKNOWN"
        End Select
        Return ErrSTRING
    End Function

    Public Function MotorError(ErrID As Int64) As String
        Dim ErrSTRING As String = ""
        Select Case ErrID
            Case = 0
                ErrSTRING = "NONE"
            Case = &H1
                ErrSTRING = "PHASE_RESISTANCE_OUT_OF_RANGE"
            Case = &H2
                ErrSTRING = "PHASE_INDUCTANCE_OUT_OF_RANGE"
            Case = &H4 'FW 0.5.1 or older
                ErrSTRING = "ADC_FAILED"
            Case = &H8
                ErrSTRING = "DRV_FAULT"
            Case = &H10
                ErrSTRING = "CONTROL_DEADLINE_MISSED"
            Case = &H20 'FW 0.5.1 or older
                ErrSTRING = "NOT_IMPLEMENTED_MOTOR_TYPE"
            Case = &H40 'FW 0.5.1 or older
                ErrSTRING = "BRAKE_CURRENT_OUT_OF_RANGE"
            Case = &H80
                ErrSTRING = "MODULATION_MAGNITUDE"
            Case = &H100 'FW 0.5.1 or older
                ErrSTRING = "BRAKE_DEADTIME_VIOLATION"
            Case = &H200 'FW 0.5.1 or older
                ErrSTRING = "UNEXPECTED_TIMER_CALLBACK"
            Case = &H400
                ErrSTRING = "CURRENT_SENSE_SATURATION"
            Case = &H800 'FW 0.5.1 or older
                ErrSTRING = "INVERTER_OVER_TEMP"
            Case = &H1000
                ErrSTRING = "CURRENT_LIMIT_VIOLATION"
            Case = &H10000
                ErrSTRING = "MODULATION_IS_NAN"
            Case = &H20000
                ErrSTRING = "MOTOR_THERMISTOR_OVER_TEMP"
            Case = &H40000
                ErrSTRING = "FET_THERMISTOR_OVER_TEMP"
            Case = &H80000
                ErrSTRING = "TIMER_UPDATE_MISSED"
            Case = &H100000
                ErrSTRING = "CURRENT_MEASUREMENT_UNAVAILABLE"
            Case = &H200000
                ErrSTRING = "CONTROLLER_FAILED"
            Case = &H400000
                ErrSTRING = "I_BUS_OUT_OF_RANGE"
            Case = &H800000
                ErrSTRING = "BRAKE_RESISTOR_DISARMED"
            Case = &H1000000
                ErrSTRING = "SYSTEM_LEVEL"
            Case = &H2000000
                ErrSTRING = "BAD_TIMING"
            Case = &H4000000
                ErrSTRING = "UNKNOWN_PHASE_ESTIMATE"
            Case = &H8000000
                ErrSTRING = "UNKNOWN_PHASE_VEL"
            Case = &H10000000
                ErrSTRING = "UNKNOWN_TORQUE"
            Case = &H20000000
                ErrSTRING = "UNKNOWN_CURRENT_COMMAND "
            Case = &H40000000
                ErrSTRING = "UNKNOWN_CURRENT_MEASUREMENT "
            Case = &H80000000
                ErrSTRING = "UNKNOWN_VBUS_VOLTAGE"
            Case = &H100000000
                ErrSTRING = "UNKNOWN_VOLTAGE_COMMAND"
            Case = &H200000000
                ErrSTRING = "UNKNOWN_GAINS"
            Case = &H400000000
                ErrSTRING = "CONTROLLER_INITIALIZING"
            Case = &H800000000
                ErrSTRING = "UNBALANCED_PHASES"
            Case Else
                ErrSTRING = "UNKNOWN (" & ErrID.ToString & ")"
        End Select
        Return ErrSTRING
    End Function

    Public Function EncoderError(ErrID As Integer) As String
        Dim ErrSTRING As String = ""
        Select Case ErrID
            Case = 0
                ErrSTRING = "NONE"
            Case = 1
                ErrSTRING = "UNSTABLE_GAIN"
            Case = 2
                ErrSTRING = "CPR_OUT_OF_RANGE"
            Case = 4
                ErrSTRING = "NO_RESPONSE"
            Case = 8
                ErrSTRING = "UNSUPPORTED_ENCODER_MODE"
            Case = 16
                ErrSTRING = "ILLEGAL_HALL_STATE"
            Case = 32
                ErrSTRING = "INDEX_NOT_FOUND_YET"
            Case = 64
                ErrSTRING = "ABS_SPI_TIMEOUT"
            Case = 128
                ErrSTRING = "ABS_SPI_COM_FAIL"
            Case = 256
                ErrSTRING = "ABS_SPI_NOT_READY"
            Case = 512
                ErrSTRING = "HALL_NOT_CALIBRATED_YET"
            Case Else
                ErrSTRING = "UNKNOWN (" & ErrID.ToString & ")"
        End Select
        Return ErrSTRING
    End Function

    Public Function GetCtrlModeName(NameID As Integer)
        Select Case NameID
            Case = CTRL_MODE_VOLTAGE_CONTROL
                Return CTRL_MODE_VOLTAGE_NAME
            Case = CTRL_MODE_CURRENT_CONTROL
                Return CTRL_MODE_CURRENT_NAME
            Case = CTRL_MODE_VELOCITY_CONTROL
                Return CTRL_MODE_VELOCITY_NAME
            Case = CTRL_MODE_POSITION_CONTROL
                Return CTRL_MODE_POSITION_NAME
            Case = CTRL_MODE_TRAJECTORY_CONTROL
                Return CTRL_MODE_TRAJECTORY_NAME
            Case Else
                Return CTRL_MODE_UNDEFINED_NAME
        End Select
    End Function

    Public Function GetEncoderModeName(NameID As Integer)
        Select Case NameID
            Case = ENCODER_MODE_INCREMENTAL
                Return ENCODER_MODE_INCREMENTAL_NAME
            Case = ENCODER_MODE_HALL
                Return ENCODER_MODE_HALL_NAME
            Case = ENCODER_MODE_ABSOLUTE
                Return ENCODER_MODE_ABSOLUTE_NAME
            Case Else
                Return ENCODER_MODE_UNDEFINED_NAME
        End Select
    End Function

    Public Function WriteCommand(DriveID As Integer, AxisID As Integer, CommandID As Integer, CommandValue As Single) As String
        Dim StrBuilder As New System.Text.StringBuilder
        StrBuilder.Clear()
        StrBuilder.Append("[0]drive(")
        StrBuilder.Append(DriveID.ToString)
        StrBuilder.Append(").write(w ")
        StrBuilder.Append(CommandString(CommandID, AxisID))
        StrBuilder.Append(" ")
        StrBuilder.Append(CommandValue.ToString)
        StrBuilder.Append(")")
        Return (StrBuilder.ToString)
    End Function

    Public Function ReadCommand(DriveID As Integer, AxisID As Integer, CommandID As Integer) As String
        Dim StrBuilder As New System.Text.StringBuilder
        StrBuilder.Clear()
        StrBuilder.Append("[0]drive(")
        StrBuilder.Append(DriveID.ToString)
        StrBuilder.Append(").write(r ")
        StrBuilder.Append(CommandString(CommandID, AxisID))
        StrBuilder.Append(")")
        Return (StrBuilder.ToString)
    End Function

    Public Function RPM_to_Pluse(RPMValue As Single, EncoderPPR As Integer) As Single
        Return Math.Truncate((RPMValue / 60.0) * Convert.ToSingle(EncoderPPR))
    End Function

    Public Sub MotorMapping(MapID As Integer, DriveNo As Integer, OutputNo As Integer, SwitchNo As Integer)
        Map_DRV(MapID) = DriveNo
        Map_OUT(MapID) = OutputNo
        Map_SW(MapID) = SwitchNo
    End Sub

End Module
