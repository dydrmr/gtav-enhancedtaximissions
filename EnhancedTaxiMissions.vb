Imports GTA
Imports GTA.Math
Imports System
Imports System.IO
Imports System.Text
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Threading.Tasks
Imports System.Collections.Generic


Public Class EnhancedTaxiMissions
    Inherits Script
    Public RND As New Random

    Public ShowDebugInfo As Boolean = False
    Public ToggleKey As Keys = Keys.L
    Public UnitsInKM As Boolean = True
    Public doAutosave As Boolean = False

    Public isMinigameActive As Boolean = False
    Public MiniGameStage As MiniGameStages = MiniGameStages.Standby

    Public ScriptStartTime As Integer = 0
    Public areSettingsLoaded As Boolean = False

    Public isSpecialMission As Boolean = False

    Public Origin, Destination As Location
    Public PotentialOrigins, PotentialDestinations As New List(Of Location)

    Public OriginBlip, DestinationBlip As Blip
    Public OriginMarker, DestinationMarker As Prop

    Public Customer As Person
    Public CustomerPed As Ped
    Public isThereASecondCustomer As Boolean = False
    Public isThereAThirdCustomer As Boolean = False
    Public Customer2Ped As Ped
    Public Customer3Ped As Ped
    Public Ped1Blip, Ped2Blip, Ped3Blip As Blip

    Public MissionStartTime As Integer = 0
    Public OriginArrivalTime As Integer = 0
    Public PickupTime As Integer = 0
    Public DestinationArrivalTime As Integer = 0

    Public IdealTripTime As Integer = 0
    Public IdealArrivalTime As Integer = 0
    Public ArrivalWindowStart As Integer = 0
    Public ArrivalWindowEnd As Integer = 0
    Public AverageSpeed As Integer = 65

    Public isCustomerPedSpawned As Boolean = False
    Public isDestinationCleared As Boolean = False
    Public isCustomerNudged1 As Boolean = False
    Public isCustomerNudged2 As Boolean = False
    Public NudgeResetTime As Integer = 0

    Public NearestLocationDistance As Integer

    Const FareBase As Integer = 6     'In Los Angeles: $2.90 base fare
    Public FarePerMile As Single = 25 'In Los Angeles: $0.30/mile
    Public FareDistance As Single = 0
    Public FareTotal As Integer = 0
    Public FareTip As Integer = 0
    Public FareTipPercent As Single = 0
    Public MaximumTip As Single = 0.4

    Public IngameMinute As Integer = 0
    Public IngameHour As Integer = 0
    Public IngameDay As Integer = 0

    Public NextMissionStartTime As Integer = 0

    Public UI As New UIContainer(New Point(40, 50), New Size(190, 80), Color.FromArgb(0, 0, 0, 0))
    Public UI_DispatchStatus As String = "DISPATCH-TEXT-INIT"
    Public UI_Origin As String = "ORIG-INIT"
    Public UI_Destination As String = "DEST-INIT"
    Public UI_Dist1 As String = "999"
    Public UI_Dist2 As String = "999"

    Public UIcolor_Header As Color = Color.FromArgb(140, 60, 140, 230)
    Public UIcolor_Status As Color = Color.FromArgb(140, 110, 190, 240)
    Public UIcolor_BG As Color = Color.FromArgb(160, 0, 0, 0)
    Public UItext_White As Color = Color.White
    Public UItext_Dark As Color = Color.FromArgb(250, 120, 120, 120)

    Public UI_Debug As New UIContainer(New Point(40, 140), New Size(190, 60), Color.FromArgb(0, 0, 0, 0))

    Public updateDist1 As Boolean = False
    Public updateDist2 As Boolean = False

    Public Enum MiniGameStages
        Standby                 '0
        DrivingToOrigin         '1
        StoppingAtOrigin        '2
        PedWalkingToCar         '3
        PedGettingInCar         '4
        DrivingToDestination    '5
        StoppingAtDestination   '6
        PedGettingOut           '7
        PedWalkingAway          '8
        SearchingForFare        '9
    End Enum

    Public Sub PRINT(msg As String)
        If ShowDebugInfo = True Then
            GTA.UI.Notify(GTA.World.CurrentDayTime.Hours.ToString("D2") & ":" & GTA.World.CurrentDayTime.Minutes.ToString("D2") & ": " & msg)
        End If
    End Sub

    Public Sub SavePosition(ByVal sender As Object, ByVal k As KeyEventArgs) Handles MyBase.KeyUp

        'Temporary subroutine that aims to save the players current XYZ coords and heading to an ini file, to speed up the process of entering coordinates for new locations.
        'Haven't quite figured out how to save to an ini file yet.

        If ShowDebugInfo = False Then Exit Sub

        If k.KeyCode = Keys.Multiply Then

            Settings.SetValue("TestSection", "TestName", "TestValue")

            Dim pos As Vector3 = Game.Player.Character.Position
            Dim hdg As Single = Game.Player.Character.Heading

            Dim positionName As String = Game.GetUserInput(64)
            Dim value1 As String = "(" & Math.Round(pos.X, 2) & ", " & Math.Round(pos.Y, 2) & ", " & Math.Round(pos.Z, 2) & ")"
            Dim value2 As String = value1 & ", " & Math.Round(hdg)

            If Game.Player.Character.IsInVehicle Then
                Settings.SetValue("POSITIONS", positionName, value1)
            Else
                Settings.SetValue("POSITIONS", positionName, value2)
            End If
        End If
    End Sub

    Public Sub ReloadSettings(ByVal sender As Object, ByVal k As KeyEventArgs) Handles MyBase.KeyUp
        'If ShowDebugInfo = False Then Exit Sub

        If k.KeyCode = Keys.Divide Then

            GTA.UI.Notify("Enhanced Taxi Missions - Reloading Settings...")

            LoadSettings()

            Dim v1 As String = Settings.GetValue("SETTINGS", "TOGGLE")
            Dim v2 As String = Settings.GetValue("SETTINGS", "UNITS")
            Dim v3 As String = Settings.GetValue("SETTINGS", "FAREPERMILE")
            Dim v4 As String = Settings.GetValue("SETTINGS", "AVERAGESPEED")
            Dim v5 As String = Settings.GetValue("SETTINGS", "AUTOSAVE")
            If v5 = "1" Then
                v5 = "YES"
            Else
                v5 = "NO"
            End If
            GTA.UI.Notify("TOGGLE: " & v1 & " | UNITS: " & v2 & " | PER MILE: $" & v3 & " | SPEED: " & v4 & " | AUTOSAVE: " & v5)

        End If
    End Sub




    Public Sub New()
        initPlaceLists()
    End Sub

    Public Sub Update(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Tick

        checkIfItsTimeToLoadSettings()
        checkIfMinigameIsActive()


        If isMinigameActive Then
            updateIngameTime()
            updateDistances()
            updateRoutes()
            updateTaxiLight()

            checkIfPlayerIsWanted()
            checkIfPlayerIsDead()

            checkIfItsTimeToStartANewMission()
            checkIfCloseEnoughToSpawnPed()
            checkIfPlayerHasArrivedAtOrigin()
            checkIfPlayerHasStoppedAtOrigin()

            resetNudgeFlags()
            checkIfPassengerNeedsToBeNudged()

            checkIfPedHasReachedCar()
            checkIfPedHasEnteredCar()
            checkIfCloseEnoughToClearDestination()
            checkIfPlayerHasArrivedAtDestination()
            checkIfPlayerHasStoppedAtDestination()

            refreshUI()
        End If

    End Sub

    Public Sub LoadSettings()
        Dim value As String = ""
        value = Settings.GetValue("SETTINGS", "TOGGLE")
        Select Case value
            Case "A"
                ToggleKey = Keys.A
            Case "B"
                ToggleKey = Keys.B
            Case "C"
                ToggleKey = Keys.C
            Case "D"
                ToggleKey = Keys.D
            Case "E"
                ToggleKey = Keys.E
            Case "F"
                ToggleKey = Keys.F
            Case "G"
                ToggleKey = Keys.G
            Case "H"
                ToggleKey = Keys.H
            Case "I"
                ToggleKey = Keys.I
            Case "J"
                ToggleKey = Keys.J
            Case "K"
                ToggleKey = Keys.K
            Case "L"
                ToggleKey = Keys.L
            Case "M"
                ToggleKey = Keys.M
            Case "N"
                ToggleKey = Keys.N
            Case "O"
                ToggleKey = Keys.O
            Case "P"
                ToggleKey = Keys.P
            Case "Q"
                ToggleKey = Keys.Q
            Case "R"
                ToggleKey = Keys.R
            Case "S"
                ToggleKey = Keys.S
            Case "T"
                ToggleKey = Keys.T
            Case "U"
                ToggleKey = Keys.U
            Case "V"
                ToggleKey = Keys.V
            Case "W"
                ToggleKey = Keys.W
            Case "X"
                ToggleKey = Keys.X
            Case "Y"
                ToggleKey = Keys.Y
            Case "Z"
                ToggleKey = Keys.Z
            Case "F1"
                ToggleKey = Keys.F1
            Case "F2"
                ToggleKey = Keys.F2
            Case "F3"
                ToggleKey = Keys.F3
            Case "F4"
                ToggleKey = Keys.F4
            Case "F5"
                ToggleKey = Keys.F5
            Case "F6"
                ToggleKey = Keys.F6
            Case "F7"
                ToggleKey = Keys.F7
            Case "F8"
                ToggleKey = Keys.F8
            Case "F9"
                ToggleKey = Keys.F9
            Case "F10"
                ToggleKey = Keys.F10
            Case "F11"
                ToggleKey = Keys.F11
            Case "F12"
                ToggleKey = Keys.F12
            Case "1"
                ToggleKey = Keys.D1
            Case "2"
                ToggleKey = Keys.D2
            Case "3"
                ToggleKey = Keys.D3
            Case "4"
                ToggleKey = Keys.D4
            Case "5"
                ToggleKey = Keys.D5
            Case "6"
                ToggleKey = Keys.D6
            Case "7"
                ToggleKey = Keys.D7
            Case "8"
                ToggleKey = Keys.D8
            Case "9"
                ToggleKey = Keys.D9
            Case "0"
                ToggleKey = Keys.D0
            Case Else
                ToggleKey = Keys.L
        End Select


        value = Settings.GetValue("SETTINGS", "UNITS")
        Select Case value
            Case "KM"
                UnitsInKM = True
            Case "MI"
                UnitsInKM = False
            Case Else
                UnitsInKM = True
        End Select

        value = Settings.GetValue("SETTINGS", "FAREPERMILE")
        FarePerMile = CInt(value)

        value = Settings.GetValue("SETTINGS", "AVERAGESPEED")
        If value = 0 Then value = 1
        AverageSpeed = CInt(value)

        value = Settings.GetValue("DEBUG", "SHOW")
        If value = 1 Then
            ShowDebugInfo = True
        Else
            ShowDebugInfo = False
        End If

        value = Settings.GetValue("SETTINGS", "AUTOSAVE")
        If value = 1 Then
            doAutosave = True
        Else
            doAutosave = False
        End If

    End Sub

    Public Sub checkIfItsTimeToLoadSettings()
        If areSettingsLoaded = False Then
            If Game.GameTime > 1000 Then
                areSettingsLoaded = True
                ListOfPeople.Remove(NonCeleb)
                LoadSettings()
            End If
        End If
    End Sub

    Public Sub refreshUI()
        UI.Items.Clear()

        '========== TITLE
        UI.Items.Add(New UIRectangle(New Point(0, 0), New Size(190, 25), UIcolor_Header))

        Dim headerText As String
        If Game.Player.Character.IsInVehicle = True And Game.Player.Character.CurrentVehicle.DisplayName = "TAXI" Then
            headerText = "Taxi Driver"
        Else
            headerText = "Limousine Driver"
        End If
        UI.Items.Add(New UIText(headerText, New Point(3, 1), 0.5, UItext_White, 1, False))

        '========== COUNTDOWN TIMER / CLOCK
        If MiniGameStage = MiniGameStages.DrivingToDestination Then
            Dim remainder As Integer = ArrivalWindowEnd - Game.GameTime
            If remainder <= 0 Then
                UI.Items.Add(New UIText(IngameHour.ToString("D2") & ":" & IngameMinute.ToString("D2"), New Point(156, 0), 0.5, UItext_White, 4, False))
            Else
                Dim s As Integer
                Dim col As Color
                s = CInt(remainder / 1000)
                If s < CInt((ArrivalWindowEnd - ArrivalWindowStart) / 1000) Then
                    col = Color.Yellow
                ElseIf s < 5 Then
                    col = Color.Red
                Else
                    col = Color.Green
                End If
                UI.Items.Add(New UIText(s.ToString, New Point(175, 0), 0.5, col, 4, True))
            End If
        Else
            UI.Items.Add(New UIText(IngameHour.ToString("D2") & ":" & IngameMinute.ToString("D2"), New Point(156, 0), 0.5, UItext_White, 4, False))
        End If



        '========== DISPATCH STATUS
        UI.Items.Add(New UIRectangle(New Point(0, 27), New Size(190, 20), UIcolor_Status))
        UI.Items.Add(New UIText(UI_DispatchStatus, New Point(3, 28), 0.35F, UItext_White, 4, False))


        '========== ORIGIN/DESTINATION INFORMATION
        UI.Items.Add(New UIRectangle(New Point(0, 47), New Size(190, 40), UIcolor_BG))

        If MiniGameStage = MiniGameStages.DrivingToOrigin Or MiniGameStage = MiniGameStages.StoppingAtOrigin Then
            UI.Items.Add(New UIText(UI_Origin & UI_Dist1, New Point(3, 48), 0.35F, UItext_White, 4, False))
        Else
            UI.Items.Add(New UIText(UI_Origin & UI_Dist1, New Point(3, 48), 0.35F, UItext_Dark, 4, False))
        End If

        If MiniGameStage = MiniGameStages.DrivingToDestination Or MiniGameStage = MiniGameStages.StoppingAtDestination Then
            UI.Items.Add(New UIText(UI_Destination & UI_Dist2, New Point(3, 68), 0.35F, UItext_White, 4, False))
        Else
            UI.Items.Add(New UIText(UI_Destination & UI_Dist2, New Point(3, 68), 0.35F, UItext_Dark, 4, False))
        End If

        UI.Draw()

        If ShowDebugInfo = True Then
            UI_Debug.Items.Clear()

            UI_Debug.Items.Add(New UIRectangle(New Point(0, 0), New Size(190, 80), UIcolor_BG))
            UI_Debug.Items.Add(New UIText("DEBUG INFORMATION", New Point(3, 0), 0.25, UItext_White, 0, False))
            UI_Debug.Items.Add(New UIText("Game Stage: " & MiniGameStage.ToString, New Point(3, 15), 0.25, UItext_White, 0, False))
            UI_Debug.Items.Add(New UIText("Date: " & World.CurrentDayTime.TotalDays, New Point(3, 30), 0.25, UItext_White, 0, False))
            Dim x As Single = GTA.Native.Function.Call(Of Single)(Native.Hash.GET_GAME_TIMER)
            UI_Debug.Items.Add(New UIText("Game Timer: " & x, New Point(3, 45), 0.25, UItext_White, 0, False))
            UI_Debug.Items.Add(New UIText("Game Time: " & Game.GameTime, New Point(3, 60), 0.25, UItext_White, 0, False))


            UI_Debug.Draw()
        End If
    End Sub




    Public Sub checkIfMinigameIsActive()
        If isMinigameActive = True Then
            UI.Enabled = True
            UI_Debug.Enabled = True
        Else
            UI.Enabled = False
            UI_Debug.Enabled = False
        End If
    End Sub

    Public Sub updateDistances()

        If isMinigameActive = False Then Exit Sub

        If Origin IsNot Nothing Then
            If updateDist1 = True Then
                Dim ppos As Vector3 = Game.Player.Character.Position
                Dim opos As Vector3 = Origin.Coords
                Dim dist As Integer = 0

                dist = GTA.Native.Function.Call(Of Single)(Native.Hash.CALCULATE_TRAVEL_DISTANCE_BETWEEN_POINTS, ppos.X, ppos.Y, ppos.Z, opos.X, opos.Y, opos.Z)
                If dist < 500 Then
                    If UnitsInKM = True Then
                        UI_Dist1 = "  (" & dist & " m)"
                    Else
                        UI_Dist1 = "  (" & Math.Round(dist * 3.28084) & " ft)"
                    End If
                Else
                    If UnitsInKM = True Then
                        UI_Dist1 = "  (" & Math.Round(dist / 1000, 2) & " km)"
                    Else
                        UI_Dist1 = "  (" & Math.Round((dist / 1000) * 0.621371, 2) & " mi)"
                    End If
                End If
            Else
                UI_Dist1 = ""
            End If
        End If

        If Destination IsNot Nothing Then
            If updateDist2 = True Then
                Dim ppos As Vector3 = Game.Player.Character.Position
                Dim dpos As Vector3 = Destination.Coords
                Dim dist As Integer = 0

                dist = GTA.Native.Function.Call(Of Single)(Native.Hash.CALCULATE_TRAVEL_DISTANCE_BETWEEN_POINTS, ppos.X, ppos.Y, ppos.Z, dpos.X, dpos.Y, dpos.Z)

                If dist < 500 Then
                    If UnitsInKM = True Then
                        UI_Dist2 = "  (" & dist & " m)"
                    Else
                        UI_Dist2 = "  (" & Math.Round(dist * 3.28084) & " ft)"
                    End If
                Else
                    If UnitsInKM = True Then
                        UI_Dist2 = "  (" & Math.Round(dist / 1000, 2) & " km)"
                    Else
                        UI_Dist2 = "  (" & Math.Round((dist / 1000) * 0.621371, 2) & " mi)"
                    End If
                End If
            Else
                UI_Dist2 = ""
            End If
        End If
    End Sub

    Public Sub updateRoutes()

        'Exit Sub

        If IngameMinute Mod 4 = 0 Then
            If MiniGameStage = MiniGameStages.DrivingToOrigin Then
                OriginBlip.ShowRoute = False
                OriginBlip.ShowRoute = True
            End If

            If MiniGameStage = MiniGameStages.DrivingToDestination Then
                DestinationBlip.ShowRoute = False
                DestinationBlip.ShowRoute = True
            End If
        End If
    End Sub

    Public Sub updateIngameTime()
        IngameHour = World.CurrentDayTime.Hours
        IngameMinute = World.CurrentDayTime.Minutes
        IngameDay = GTA.Native.Function.Call(Of Integer)(Native.Hash.GET_CLOCK_DAY_OF_WEEK)
    End Sub

    Public Sub updateTaxiLight()
        If Game.Player.Character.IsInVehicle Then
            If Game.Player.Character.CurrentVehicle.DisplayName = "TAXI" Then
                If MiniGameStage = MiniGameStages.DrivingToDestination Then
                    Game.Player.Character.CurrentVehicle.TaxiLightOn = False
                Else
                    Game.Player.Character.CurrentVehicle.TaxiLightOn = True
                End If
            End If
        End If
    End Sub

    Public Sub resetNudgeFlags()
        If isCustomerNudged1 = True Then
            If Game.GameTime > NudgeResetTime Then
                isCustomerNudged1 = False
            End If
        End If

        If isCustomerNudged2 = True Then
            If Game.GameTime > NudgeResetTime Then
                isCustomerNudged2 = False
            End If
        End If
    End Sub

    Public Sub checkIfItsTimeToStartANewMission()
        If MiniGameStage = MiniGameStages.SearchingForFare Then
            If Game.GameTime > NextMissionStartTime Then
                NextMissionStartTime = 0
                StartMinigame()
            End If
        End If
    End Sub

    Public Sub checkIfPlayerIsWanted()
        If Game.Player.WantedLevel > 0 Then
            EndMinigame(True)
        End If
    End Sub

    Public Sub checkIfPlayerIsDead()
        If Game.Player.IsDead Then
            EndMinigame(True)
        End If
    End Sub

    Public Sub checkIfCloseEnoughToSpawnPed()
        If MiniGameStage = MiniGameStages.DrivingToOrigin Then
            Dim ppos As Vector3 = Game.Player.Character.Position
            Dim opos As Vector3 = Origin.Coords
            Dim distance As Single = World.GetDistance(ppos, opos)

            If distance < 130 Then
                Dim pos As Vector3 = Origin.PedStart
                If isCustomerPedSpawned = False Then
                    isCustomerPedSpawned = True

                    GTA.Native.Function.Call(Native.Hash.CLEAR_AREA_OF_PEDS, pos.X, pos.Y, pos.Z, 30)

                    If Customer.isCeleb = True Then
                        CustomerPed = World.CreatePed(New GTA.Model(Customer.Model), Origin.PedStart, Origin.PedStartHDG)
                    Else
                        CustomerPed = GTA.Native.Function.Call(Of Ped)(Native.Hash.CREATE_RANDOM_PED, pos.X, pos.Y, pos.Z + 0.3)
                        If CustomerPed.Exists Then
                            CustomerPed.Heading = Origin.PedStartHDG
                            CustomerPed.Money = RND.Next(10, 200)
                        End If
                    End If

                    'TO-DO
                    'PUT PEDS INTO A GROUP OR SET THEIR RELATIONSHIPS TO FRIENDLY SO THEY DON'T PANIC WHEN THEY ALL GET INTO THE CAR

                    If isThereASecondCustomer = True Then
                        Customer2Ped = GTA.Native.Function.Call(Of Ped)(Native.Hash.CREATE_RANDOM_PED, pos.X + 0.2, pos.Y + 0.2, pos.Z + 0.3)
                        Customer2Ped.RelationshipGroup = 1
                        Customer2Ped.Money = RND.Next(10, 200)
                    End If

                    If isThereAThirdCustomer = True Then
                        Customer3Ped = GTA.Native.Function.Call(Of Ped)(Native.Hash.CREATE_RANDOM_PED, pos.X - 0.2, pos.Y - 0.2, pos.Z + 0.3)
                        Customer3Ped.RelationshipGroup = 1
                        Customer3Ped.Money = RND.Next(10, 200)
                    End If

                    'TO-DO
                    'CREATE MISSION MARKER (LIKE THE ACTUAL TAXI MISSIONS)

                End If
            End If
        End If
    End Sub

    Public Sub checkIfPedsAreAlive()
        Dim c1 As Boolean = True
        Dim c2 As Boolean = True
        Dim c3 As Boolean = True

        If CustomerPed.IsDead Then
            c1 = False
        End If

        If isThereASecondCustomer = True Then
            If Customer2Ped.IsDead Then
                c2 = False
            End If
        End If

        If isThereAThirdCustomer = True Then
            If Customer3Ped.IsDead Then
                c3 = False
            End If
        End If

        If c1 = False Or c2 = False Or c3 = False Then
            CustomerHasDied()
        End If

    End Sub

    Public Sub checkIfPlayerHasArrivedAtOrigin()
        If MiniGameStage = MiniGameStages.DrivingToOrigin Then

            Dim ppos As Vector3 = Game.Player.Character.Position
            Dim opos As Vector3 = Origin.Coords

            Dim distance As Single = GTA.Native.Function.Call(Of Single)(Native.Hash.GET_DISTANCE_BETWEEN_COORDS, ppos.X, ppos.Y, ppos.Z, opos.X, opos.Y, opos.Z, 1)

            If distance < 9 Then
                PlayerHasArrivedAtOrigin()
            End If
        End If
    End Sub

    Public Sub checkIfPlayerHasStoppedAtOrigin()
        If MiniGameStage = MiniGameStages.StoppingAtOrigin Then
            If Game.Player.Character.IsInVehicle = True Then
                If Game.Player.Character.CurrentVehicle.Speed = 0 Then
                    If World.GetDistance(Game.Player.Character.Position, Origin.Coords) < 70 Then
                        PlayerHasStoppedAtOrigin()
                    End If
                End If
            End If
        End If
    End Sub

    Public Sub checkIfPassengerNeedsToBeNudged()
        Dim isHonking As Boolean
        isHonking = Game.Player.IsPressingHorn

        If MiniGameStage = MiniGameStages.PedWalkingToCar Then
            If isHonking = True Then
                If isCustomerNudged1 = False Then
                    If CustomerPed.Exists Then
                        CustomerPed.Position = CustomerPed.Position + CustomerPed.ForwardVector * 2
                        CustomerPed.Task.GoTo(Game.Player.Character.Position, False)

                        If isThereASecondCustomer = True Then
                            If Customer2Ped IsNot Nothing Then
                                If Customer2Ped.Exists = True Then
                                    Customer2Ped.Position = Customer2Ped.Position + Customer2Ped.ForwardVector * 2
                                    Customer2Ped.Task.GoTo(Game.Player.Character.Position, False)
                                End If
                            End If
                        End If

                        If isThereAThirdCustomer = True Then
                            If Customer3Ped IsNot Nothing Then
                                If Customer3Ped.Exists = True Then
                                    Customer3Ped.Position = Customer3Ped.Position + Customer3Ped.ForwardVector * 2
                                    Customer3Ped.Task.GoTo(Game.Player.Character.Position, False)
                                End If
                            End If
                        End If

                        isCustomerNudged1 = True
                        NudgeResetTime = Game.GameTime + 750
                    End If
                End If
            End If
        End If



        If MiniGameStage = MiniGameStages.PedGettingInCar Then
            If isHonking = True Then
                If isCustomerNudged2 = False Then
                    If CustomerPed.Exists Then
                        CustomerPed.Task.EnterVehicle(Game.Player.Character.CurrentVehicle, VehicleSeat.RightRear, 8000)
                    End If
                    If isThereASecondCustomer = True Then
                        If Customer2Ped.Exists Then
                            Customer2Ped.Task.EnterVehicle(Game.Player.Character.CurrentVehicle, VehicleSeat.LeftRear, 8000)
                        End If
                    End If
                    If isThereAThirdCustomer = True Then
                        If Customer3Ped.Exists Then
                            Customer3Ped.Task.EnterVehicle(Game.Player.Character.CurrentVehicle, VehicleSeat.Passenger, 8000)
                        End If
                    End If
                    isCustomerNudged2 = True
                    NudgeResetTime = Game.GameTime + 750
                End If
            End If
        End If
    End Sub

    Public Sub checkIfPedHasReachedCar()
        If MiniGameStage = MiniGameStages.PedWalkingToCar Then
            If CustomerPed IsNot Nothing Then
                If CustomerPed.Exists = True Then
                    Dim tgt As Vector3 = Game.Player.Character.Position
                    Dim ppo As Vector3 = CustomerPed.Position
                    Dim distance As Single = World.GetDistance(tgt, ppo)

                    If distance < 9 Then
                        PedHasReachedCar()
                    End If
                End If
            End If
        End If
    End Sub

    Public Sub checkIfPedHasEnteredCar()

        If MiniGameStage = MiniGameStages.PedGettingInCar Then
            If CustomerPed.Exists = True Then
                If Game.Player.Character.IsInVehicle Then
                    Dim isPed1Sitting As Boolean = GTA.Native.Function.Call(Of Boolean)(Native.Hash.IS_PED_IN_VEHICLE, CustomerPed, Game.Player.Character.CurrentVehicle, False)
                    Dim isPed2Sitting As Boolean
                    Dim isPed3Sitting As Boolean

                    If isThereASecondCustomer = True Then
                        isPed2Sitting = GTA.Native.Function.Call(Of Boolean)(Native.Hash.IS_PED_IN_VEHICLE, Customer2Ped, Game.Player.Character.CurrentVehicle, False)
                    Else
                        isPed2Sitting = True
                    End If

                    If isThereAThirdCustomer = True Then
                        isPed3Sitting = GTA.Native.Function.Call(Of Boolean)(Native.Hash.IS_PED_IN_VEHICLE, Customer3Ped, Game.Player.Character.CurrentVehicle, False)
                    Else
                        isPed3Sitting = True
                    End If

                    Dim areAllPedsSitting As Boolean = isPed1Sitting And isPed2Sitting And isPed3Sitting
                    If areAllPedsSitting = True Then
                        PedHasEnteredCar()
                    End If
                End If
            End If
        End If


        'BOOL IS_PED_IN_VEHICLE(Ped pedHandle, Vehicle vehicleHandle, BOOL atGetIn) // 0x7DA6BC83
        'Gets a value indicating whether the specified ped is in the specified vehicle.
        'If 'atGetIn' is false, the function will not return true until the ped is sitting in the vehicle and is about to close the door. If it's true, the function returns true 
        'the moment the ped starts to get onto the seat (after opening the door). Eg. if false, and the ped is getting into a submersible, the function will not return true until             
        'the ped has descended down into the submersible and gotten into the seat, while if it's true, it'll return true the moment the hatch has been opened and the ped is about 
        'to descend into the submersible.

    End Sub

    Public Sub checkIfCloseEnoughToClearDestination()
        If MiniGameStage = MiniGameStages.DrivingToDestination Then
            Dim ppos As Vector3 = Game.Player.Character.Position
            Dim dpos As Vector3 = Destination.Coords
            Dim distance As Single = World.GetDistance(ppos, dpos)

            If distance < 90 Then
                Dim pos As Vector3 = Destination.PedStart
                If isDestinationCleared = False Then
                    isDestinationCleared = True

                    GTA.Native.Function.Call(Native.Hash.CLEAR_AREA_OF_PEDS, pos.X, pos.Y, pos.Z, 30)

                    'TO-DO
                    'ADD DESTINATION MISSION MARKER
                End If
            End If

        End If
    End Sub

    Public Sub checkIfPlayerHasArrivedAtDestination()
        If MiniGameStage = MiniGameStages.DrivingToDestination Then

            Dim ppos As Vector3 = Game.Player.Character.Position
            Dim opos As Vector3 = Destination.Coords

            Dim distance As Single = GTA.Native.Function.Call(Of Single)(Native.Hash.GET_DISTANCE_BETWEEN_COORDS, ppos.X, ppos.Y, ppos.Z, opos.X, opos.Y, opos.Z)

            If distance < 9 Then
                PlayerHasArrivedAtDestination()
            End If
        End If
    End Sub

    Public Sub checkIfPlayerHasStoppedAtDestination()
        If MiniGameStage = MiniGameStages.StoppingAtDestination Then
            If Game.Player.Character.IsInVehicle = True Then
                If Game.Player.Character.CurrentVehicle.Speed = 0 Then
                    If World.GetDistance(Game.Player.Character.Position, Destination.Coords) < 70 Then
                        PlayerHasStoppedAtDestination()
                    End If
                End If
            End If
        End If
    End Sub









    Public Sub ToggleMinigame(ByVal sender As Object, ByVal k As KeyEventArgs) Handles MyBase.KeyUp

        If Game.Player.IsOnMission = True Then Exit Sub

        If k.KeyCode = ToggleKey Then

            If isMinigameActive = True Then
                EndMinigame()
                isMinigameActive = False
            Else

                If Game.Player.Character.IsInVehicle Then
                    Dim maxSeats As Integer = GTA.Native.Function.Call(Of Integer)(Native.Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, Game.Player.Character.CurrentVehicle)

                    If maxSeats >= 3 Then
                        Dim veh As String = Game.Player.Character.CurrentVehicle.DisplayName

                        If veh = "TAXI" Or veh = "STRETCH" Or veh = "SCHAFTER" Or veh = "SUPERD" Or veh = "ORACLE" Or veh = "WASHINGT" Then
                            StartMinigame()
                            isMinigameActive = True
                        Else
                            GTA.UI.Notify("Taxi missions can be started in a Taxi, Stretch, Schafter, Super Drop, Oracle, or Washington.")
                        End If

                    Else
                        GTA.UI.Notify("Taxi missions can be started in a Taxi, Stretch, Schafter, Super Drop, Oracle, or Washington.")
                    End If

                Else
                    GTA.UI.Notify("Taxi missions can be started in a Taxi, Stretch, Schafter, Super Drop, Oracle, or Washington.")
                End If

            End If
        End If

    End Sub

    Public Sub StartMinigame()
        isMinigameActive = True

        updateDist1 = False
        updateDist2 = False

        isSpecialMission = False
        isCustomerPedSpawned = False
        isDestinationCleared = False
        isCustomerNudged1 = False
        isCustomerNudged2 = False
        isThereASecondCustomer = False
        isThereAThirdCustomer = False

        MissionStartTime = 0
        OriginArrivalTime = 0
        DestinationArrivalTime = 0
        PickupTime = 0
        IdealTripTime = 0

        NearestLocationDistance = 100000

        FareTotal = 0
        FareTip = 0

        UI_DispatchStatus = "Standby..."
        UI_Destination = ""
        UI_Origin = ""
        UI_Dist1 = 0
        UI_Dist2 = 0

        MiniGameStage = MiniGameStages.Standby

        StartMission()
    End Sub

    Public Sub EndMinigame(Optional panic As Boolean = False)

        If OriginBlip IsNot Nothing Then
            If OriginBlip.Exists Then
                OriginBlip.ShowRoute = False
                OriginBlip.Remove()
            End If
        End If

        If DestinationBlip IsNot Nothing Then
            If DestinationBlip.Exists Then
                DestinationBlip.ShowRoute = False
                DestinationBlip.Remove()
            End If
        End If

        If CustomerPed IsNot Nothing Then
            If CustomerPed.Exists Then
                If Game.Player.Character.IsInVehicle = True Then
                    CustomerPed.Task.LeaveVehicle(Game.Player.Character.CurrentVehicle, False)
                End If
                If panic = True Then
                    CustomerPed.Task.FleeFrom(Game.Player.Character)
                End If
                CustomerPed.MarkAsNoLongerNeeded()
                If Ped1Blip IsNot Nothing Then
                    If Ped1Blip.Exists Then
                        Ped1Blip.Remove()
                        Ped1Blip = Nothing
                    End If
                End If
            End If
        End If

        If Customer2Ped IsNot Nothing Then
            If Customer2Ped.Exists = True Then
                If Game.Player.Character.IsInVehicle = True Then
                    Customer2Ped.Task.LeaveVehicle(Game.Player.Character.CurrentVehicle, True)
                End If
                If panic = True Then
                    CustomerPed.Task.FleeFrom(Game.Player.Character)
                End If
                Customer2Ped.MarkAsNoLongerNeeded()
                If Ped2Blip IsNot Nothing Then
                    If Ped2Blip.Exists Then
                        Ped2Blip.Remove()
                        Ped2Blip = Nothing
                    End If
                End If
            End If
        End If

        If Customer3Ped IsNot Nothing Then
            If Customer3Ped.Exists = True Then
                If Game.Player.Character.IsInVehicle = True Then
                    Customer3Ped.Task.LeaveVehicle(Game.Player.Character.CurrentVehicle, True)
                End If
                If panic = True Then
                    Customer3Ped.Task.FleeFrom(Game.Player.Character)
                End If
                Customer3Ped.MarkAsNoLongerNeeded()
                If Ped3Blip IsNot Nothing Then
                    If Ped3Blip.Exists Then
                        Ped3Blip.Remove()
                        Ped3Blip = Nothing
                    End If
                End If
            End If
        End If

        isMinigameActive = False

    End Sub

    Public Sub payPlayer(amount As Integer)
        Dim currentMoney = Game.Player.Money
        Game.Player.Money = currentMoney + amount

        GTA.Native.Function.Call(Native.Hash.DISPLAY_CASH, True)
    End Sub

    Public Sub calculateFare(StartPoint As Vector3, EndPoint As Vector3)
        Dim sPos As Vector3 = StartPoint
        Dim ePos As Vector3 = EndPoint
        FareDistance = GTA.Native.Function.Call(Of Single)(Native.Hash.CALCULATE_TRAVEL_DISTANCE_BETWEEN_POINTS, sPos.X, sPos.Y, sPos.Z, ePos.X, ePos.Y, ePos.Z) / 1000
        If FareDistance > 20 Then
            FareDistance = World.GetDistance(StartPoint, EndPoint) / 1000
        End If

        IdealTripTime = (FareDistance / AverageSpeed) * 60 * 60 * 1000

        FareDistance *= 0.621371
        FareTotal = CInt(Math.Round(FareBase + (FareDistance * FarePerMile)))
    End Sub

    Public Sub calculateTipParameters()
        ArrivalWindowEnd = PickupTime + IdealTripTime - (IdealTripTime * 0.05)
        PRINT("Ideal Trip Time: " & Math.Round(IdealTripTime / 1000) & " seconds")

        ArrivalWindowStart = Math.Round(ArrivalWindowEnd - (IdealTripTime * 0.5))
    End Sub

    Public Sub calculateTip()

        If DestinationArrivalTime < ArrivalWindowStart Then
            PRINT("Arrived early")
            FareTipPercent = MaximumTip
        ElseIf DestinationArrivalTime > ArrivalWindowEnd Then
            PRINT("Arrived too late")
            FareTipPercent = 0
        Else

            Dim span As Integer = ArrivalWindowEnd - ArrivalWindowStart
            Dim arr As Integer = DestinationArrivalTime - ArrivalWindowStart
            Dim pct As Single = arr / span
            FareTipPercent = MaximumTip - (MaximumTip * pct)
        End If
        PRINT("Percent " & FareTipPercent & " ARR: " & DestinationArrivalTime - ArrivalWindowStart)

        FareTip = FareTotal * FareTipPercent
    End Sub





    Private Sub StartMission()

        MissionStartTime = Game.GameTime

        Dim r As Integer = RND.Next(0, 10)
        If r < 1 Then
            isSpecialMission = True
            GenerateSpecialMissionLocations()
        Else
            isSpecialMission = False
            GenerateGenericMissionLocations()
        End If

        SelectValidOrigin(PotentialOrigins)

        If Origin.Type = LocationType.AirportArrive Then
            Dim l As New List(Of Location)
            l.AddRange(lResidential)
            l.AddRange(lHotelLS)
            l.AddRange(lMotelLS)
            l.AddRange(lOffice)
            SelectValidDestination(l)
        Else
            SelectValidDestination(PotentialDestinations)
        End If

        If isSpecialMission = True Then
            SelectSpecialCustomers()
        Else
            SelectGenericCustomers()
        End If

        OriginBlip = World.CreateBlip(Origin.Coords)
        OriginBlip.Color = BlipColor.Blue
        OriginBlip.ShowRoute = True

        updateDist1 = True
        MiniGameStage = MiniGameStages.DrivingToOrigin
    End Sub

    Private Sub GenerateGenericMissionLocations()

        PotentialOrigins.Clear()
        PotentialDestinations.Clear()

        Select Case IngameHour
            Case 0 To 5
                With PotentialOrigins
                    .AddRange(lAirportA)
                    .AddRange(lBar)
                    .AddRange(lMotelLS)
                    .AddRange(lStripClub)
                    .AddRange(lTheater)
                End With
                With PotentialDestinations
                    .AddRange(lHotelLS)
                    .AddRange(lResidential)
                    .AddRange(lBar)
                    .AddRange(lMotelLS)
                    .AddRange(lStripClub)
                End With

            Case 6 To 10
                With PotentialOrigins
                    .AddRange(lResidential)
                    .AddRange(lHotelLS)
                    .AddRange(lMotelLS)
                    .AddRange(lSport)
                    .AddRange(lFastFood)
                    .AddRange(lAirportA)
                    .AddRange(lShopping)
                    .AddRange(lReligious)
                    .AddRange(lStripClub)
                End With
                With PotentialDestinations
                    .AddRange(lAirportD)
                    .AddRange(lHotelLS)
                    .AddRange(lMotelLS)
                    .AddRange(lSport)
                    .AddRange(lEntertainment)
                    .AddRange(lFastFood)
                    .AddRange(lShopping)
                    .AddRange(lReligious)
                    .AddRange(lOffice)
                    .AddRange(lSchool)
                End With

            Case 11 To 15
                With PotentialOrigins
                    .AddRange(lAirportA)
                    .AddRange(lHotelLS)
                    .AddRange(lMotelLS)
                    .AddRange(lSport)
                    .AddRange(lEntertainment)
                    .AddRange(lFastFood)
                    .AddRange(lResidential)
                    .AddRange(lRestaurant)
                    .AddRange(lReligious)
                    .AddRange(lShopping)
                    .AddRange(lOffice)
                    .AddRange(lSchool)
                End With
                With PotentialDestinations
                    .AddRange(lAirportD)
                    .AddRange(lHotelLS)
                    .AddRange(lMotelLS)
                    .AddRange(lSport)
                    .AddRange(lEntertainment)
                    .AddRange(lFastFood)
                    .AddRange(lResidential)
                    .AddRange(lRestaurant)
                    .AddRange(lReligious)
                    .AddRange(lShopping)
                    .AddRange(lOffice)
                    .AddRange(lTheater)
                    .AddRange(lSchool)
                End With

            Case 16 To 19
                With PotentialOrigins
                    .AddRange(lAirportA)
                    .AddRange(lHotelLS)
                    .AddRange(lMotelLS)
                    .AddRange(lSport)
                    .AddRange(lEntertainment)
                    .AddRange(lFastFood)
                    .AddRange(lRestaurant)
                    .AddRange(lShopping)
                    .AddRange(lReligious)
                    .AddRange(lOffice)
                    .AddRange(lTheater)
                    .AddRange(lSchool)
                End With
                With PotentialDestinations
                    .AddRange(lAirportD)
                    .AddRange(lHotelLS)
                    .AddRange(lMotelLS)
                    .AddRange(lEntertainment)
                    .AddRange(lFastFood)
                    .AddRange(lResidential)
                    .AddRange(lRestaurant)
                    .AddRange(lShopping)
                    .AddRange(lBar)
                    .AddRange(lTheater)
                    .AddRange(lSchool)
                End With

            Case 20 To 23
                With PotentialOrigins
                    .AddRange(lAirportA)
                    .AddRange(lHotelLS)
                    .AddRange(lMotelLS)
                    .AddRange(lEntertainment)
                    .AddRange(lFastFood)
                    .AddRange(lResidential)
                    .AddRange(lRestaurant)
                    .AddRange(lShopping)
                    .AddRange(lBar)
                    .AddRange(lStripClub)
                    .AddRange(lTheater)
                    .AddRange(lSchool)
                End With
                With PotentialDestinations
                    .AddRange(lResidential)
                    .AddRange(lHotelLS)
                    .AddRange(lMotelLS)
                    .AddRange(lEntertainment)
                    .AddRange(lFastFood)
                    .AddRange(lResidential)
                    .AddRange(lShopping)
                    .AddRange(lBar)
                    .AddRange(lStripClub)
                    .AddRange(lTheater)
                End With

            Case Else
                PotentialOrigins = ListOfPlaces
                PotentialDestinations = ListOfPlaces

        End Select
    End Sub

    Private Sub GenerateSpecialMissionLocations()

        '\/  \/  \/  TEMPORARY!  \/  \/  \/
        GenerateGenericMissionLocations()
        '/\  /\  /\  TEMPORARY!  /\  /\  /\

        'EPSILON
        'CELEB
        'HURRY
        'FOLLOWTHATCAR

    End Sub

    Private Sub SelectValidOrigin(Places As List(Of Location))

        If Places.Count = 0 Then Places.AddRange(ListOfPlaces)

        Dim NearestLocation As Location
        For Each l As Location In Places
            Dim ppos As Vector3 = Game.Player.Character.Position
            Dim dist As Single
            dist = World.GetDistance(l.Coords, ppos)
            If dist < NearestLocationDistance Then
                NearestLocation = l
                NearestLocationDistance = dist
            End If
        Next




        If NearestLocationDistance > 500 Then
            NearestLocationDistance += 80%
        Else
            NearestLocationDistance = 500
        End If



        Dim r As Integer
        Dim distance As Single
        Dim c As Integer = 0
        Do
            r = RND.Next(0, Places.Count)
            Origin = Places(r)
            Dim ppos As Vector3 = Game.Player.Character.Position
            distance = World.GetDistance(Origin.Coords, ppos)
            c += 1
            If c > 10 Then
                r = RND.Next(0, ListOfPlaces.Count)
                Origin = ListOfPlaces(r)
                distance = World.GetDistance(Origin.Coords, ppos)
            End If
        Loop While distance > NearestLocationDistance Or distance < 50

        UI_Origin = Origin.Name
    End Sub

    Private Sub SelectValidDestination(Places As List(Of Location))
        PRINT("Valid Destinations: " & Places.Count)
        If Places.Count = 0 Then Places.AddRange(ListOfPlaces)

        Dim r As Integer
        Dim distance As Single
        Dim c As Integer = 0
        Do
            r = RND.Next(0, Places.Count)
            Destination = Places(r)
            distance = World.GetDistance(Origin.Coords, Destination.Coords)
            c += 1
            If c > 10 Then
                r = RND.Next(0, ListOfPlaces.Count)
                Destination = ListOfPlaces(r)
                distance = World.GetDistance(Origin.Coords, Destination.Coords)
                Exit Do
            End If
        Loop While Origin.Name = Destination.Name Or distance < 450 Or Origin.isValidDestination = False

        UI_Destination = Destination.Name
    End Sub

    Private Sub SelectGenericCustomers()
        Dim r As Integer

        r = RND.Next(0, 100)
        If r <= 10 Then
            Customer = ListOfPeople(r)
        Else
            Customer = NonCeleb
        End If

        r = RND.Next(0, 3)
        If r = 0 Then
            isThereASecondCustomer = True

            Dim t As Integer = RND.Next(0, 3)
            If t = 0 Then
                isThereAThirdCustomer = True
            End If
        End If




        If Customer.isCeleb = True Then
            UI_DispatchStatus = Customer.Name & " waiting for pickup"
        Else
            UI_DispatchStatus = "Customer waiting for pickup"
            If isThereASecondCustomer = True Then
                UI_DispatchStatus = "Customers waiting for pickup"
            End If
        End If
    End Sub

    Private Sub SelectSpecialCustomers()
        '\/  \/  \/  TEMPORARY!  \/  \/  \/
        SelectGenericCustomers()
        '/\  /\  /\  TEMPORARY!  /\  /\  /\
    End Sub

    Private Sub PlayerHasArrivedAtOrigin()
        updateDist1 = False
        updateDist2 = True
        OriginArrivalTime = Game.GameTime
        UI_DispatchStatus = "Please stop at the marker"
        PRINT("Orig Arr Time: " & OriginArrivalTime & " / Time taken: " & Math.Round((OriginArrivalTime - MissionStartTime) / 1000, 1))
        MiniGameStage = MiniGameStages.StoppingAtOrigin
    End Sub

    Private Sub PlayerHasStoppedAtOrigin()

        Dim ppos As Vector3
        If Game.Player.Character.IsInVehicle Then
            ppos = Game.Player.Character.CurrentVehicle.Position
        Else
            ppos = Game.Player.Character.Position
        End If

        If CustomerPed IsNot Nothing Then
            If CustomerPed.Exists Then
                CustomerPed.Task.GoTo(ppos, False)
                Ped1Blip = CustomerPed.AddBlip
                Ped1Blip.Color = BlipColor.Green
                Ped1Blip.Scale = 0.6
            End If
        End If

        If isThereASecondCustomer = True Then
            If Customer2Ped IsNot Nothing Then
                If Customer2Ped.Exists = True Then
                    Customer2Ped.Task.GoTo(ppos, False)
                    Ped2Blip = Customer2Ped.AddBlip
                    Ped2Blip.Color = BlipColor.Green
                    Ped2Blip.Scale = 0.6
                End If
            End If
        End If

        If isThereAThirdCustomer = True Then
            If Customer3Ped IsNot Nothing Then
                If Customer3Ped.Exists = True Then
                    Customer3Ped.Task.GoTo(ppos, False)
                    Ped3Blip = Customer3Ped.AddBlip
                    Ped3Blip.Color = BlipColor.Green
                    Ped3Blip.Scale = 0.6
                End If
            End If
        End If

        OriginBlip.Remove()

        'TO-DO
        'REMOVE ORIGIN MISSION MARKER

        calculateFare(Origin.Coords, Destination.Coords)

        If isThereASecondCustomer = True Then
            UI_DispatchStatus = "Customers have been notified of your arrival"
        Else
            If Customer.isCeleb Then
                UI_DispatchStatus = Customer.Name & " has been notified of your arrival"
            Else
                UI_DispatchStatus = "Customer has been notified of your arrival"
            End If
        End If

        MiniGameStage = MiniGameStages.PedWalkingToCar

    End Sub

    Private Sub PedHasReachedCar()
        If CustomerPed.Exists Then
            CustomerPed.Task.EnterVehicle(Game.Player.Character.CurrentVehicle, 2, 16000)
        End If

        If isThereASecondCustomer = True Then
            If Customer2Ped.Exists = True Then
                Customer2Ped.Task.EnterVehicle(Game.Player.Character.CurrentVehicle, 1, 16000)
            End If
        End If

        If isThereAThirdCustomer = True Then
            If Customer3Ped.Exists = True Then
                Customer3Ped.Task.EnterVehicle(Game.Player.Character.CurrentVehicle, 0, 16000)
            End If
        End If

        MiniGameStage = MiniGameStages.PedGettingInCar
    End Sub

    Private Sub PedHasEnteredCar()
        PickupTime = Game.GameTime

        calculateTipParameters()

        DestinationBlip = World.CreateBlip(Destination.Coords)
        DestinationBlip.Color = BlipColor.Blue
        DestinationBlip.ShowRoute = True

        If CustomerPed IsNot Nothing Then
            If CustomerPed.Exists Then
                Ped1Blip.Remove()
            End If
        End If

        If isThereASecondCustomer = True Then
            If Customer2Ped IsNot Nothing Then
                If Customer2Ped.Exists Then
                    Ped2Blip.Remove()
                End If
            End If
        End If

        If isThereAThirdCustomer = True Then
            If Customer3Ped IsNot Nothing Then
                If Customer3Ped.Exists Then
                    Ped3Blip.Remove()
                End If
            End If
        End If


        UI_DispatchStatus = "Please drive the customer to the destination"
        If isThereASecondCustomer = True Then
            UI_DispatchStatus = "Please drive the customers to their destination"
        End If
        MiniGameStage = MiniGameStages.DrivingToDestination
    End Sub

    Private Sub PlayerHasArrivedAtDestination()
        updateDist2 = False
        DestinationArrivalTime = Game.GameTime
        calculateTip()
        UI_DispatchStatus = "Please stop at the marker"
        MiniGameStage = MiniGameStages.StoppingAtDestination
    End Sub

    Private Sub PlayerHasStoppedAtDestination()

        MiniGameStage = MiniGameStages.PedGettingOut

        DestinationBlip.Remove()

        'TO-DO
        'REMOVE DESTINATION MISSION MARKER

        Dim LeaveSequence As New TaskSequence

        LeaveSequence.AddTask.LeaveVehicle(Game.Player.Character.CurrentVehicle, True)
        LeaveSequence.AddTask.Wait(300)

        Dim isDestinationSet As Boolean
        If Destination.PedEnd.X = 0 And Destination.PedEnd.Y = 0 And Destination.PedEnd.Z = 0 Then
            isDestinationSet = False
        Else
            isDestinationSet = True
        End If


        If isDestinationSet = False Then
            LeaveSequence.AddTask.GoTo(Destination.PedStart, False)
        Else
            LeaveSequence.AddTask.GoTo(Destination.PedEnd, False)
        End If
        LeaveSequence.AddTask.Wait(25000)


        If CustomerPed IsNot Nothing Then
            If CustomerPed.Exists = True Then
                CustomerPed.Task.PerformSequence(LeaveSequence)
                CustomerPed.MarkAsNoLongerNeeded()
            End If
        End If

        If isThereASecondCustomer = True Then
            If Customer2Ped IsNot Nothing Then
                If Customer2Ped.Exists Then
                    Customer2Ped.Task.PerformSequence(LeaveSequence)
                    Customer2Ped.MarkAsNoLongerNeeded()
                End If
            End If
        End If


        If isThereAThirdCustomer = True Then
            If Customer3Ped IsNot Nothing Then
                If Customer3Ped.Exists Then
                    Customer3Ped.Task.PerformSequence(LeaveSequence)
                    Customer3Ped.MarkAsNoLongerNeeded()
                End If
            End If
        End If

        payPlayer(FareTotal)
        GTA.UI.Notify("Fare earned: $" & FareTotal)

        If FareTip > 0 Then
            payPlayer(FareTip)
            GTA.UI.Notify("Tip received: $" & FareTip & "  (" & Math.Round(FareTipPercent * 100) & "%)")
        End If

        If doAutosave = True Then
            GTA.Native.Function.Call(Native.Hash.DO_AUTO_SAVE)
        End If

        MiniGameStage = MiniGameStages.SearchingForFare
        setStandbySpecs()
    End Sub

    Public Sub setStandbySpecs()
        UI_DispatchStatus = "Standby, looking for fares..."
        UI_Destination = ""
        UI_Origin = ""


        Dim maxWaitTime As Integer = 3
        Select Case IngameDay
            Case Is > 4
                Select Case IngameHour
                    Case 0 To 3
                        maxWaitTime = 4
                    Case 4 To 5
                        maxWaitTime = 30
                    Case 6 To 10
                        maxWaitTime = 5
                    Case 11 To 13
                        maxWaitTime = 10
                    Case 14 To 17
                        maxWaitTime = 4
                    Case 18 To 23
                        maxWaitTime = 3
                End Select
            Case Else
                Select Case IngameHour
                    Case 0
                        maxWaitTime = 9
                    Case 1 To 5
                        maxWaitTime = 30
                    Case 6 To 10
                        maxWaitTime = 5
                    Case 11 To 14
                        maxWaitTime = 11
                    Case 15 To 18
                        maxWaitTime = 5
                    Case 19 To 22
                        maxWaitTime = 4
                    Case 23
                        maxWaitTime = 8
                End Select
        End Select


        Dim r As Integer = RND.Next(500, maxWaitTime * 1000)
        NextMissionStartTime = Game.GameTime + r

    End Sub

    Private Sub CustomerHasDied()
        GTA.UI.Notify("Customer has died. You cannot complete this fare.")
        EndMinigame(True)
    End Sub
End Class



'PEDS

Public Class Person
    Public Name As String = ""
    Public Model As String = ""
    Public isCeleb As Boolean = False

    Public Sub New(n As String, mdl As String, celeb As Boolean)
        Name = n
        Model = mdl
        isCeleb = celeb
        ListOfPeople.Add(Me)
    End Sub
End Class

Public Module People
    Public ListOfPeople As New List(Of Person)

    Public PoppyMitchell As New Person("Poppy Mitchell", "u_f_y_poppymich", True)
    Public PamelaDrake As New Person("Pamela Drake", "u_f_o_moviestar", True)
    Public MirandaCowan As New Person("Miranda Cowan", "u_f_m_miranda", True)
    Public AlDiNapoli As New Person("Al Di Napoli", "u_m_m_aldinapoli", True)
    Public MarkFostenburg As New Person("Mark Fostenburg", "u_m_m_markfost", True)
    Public WillyMcTavish As New Person("Willy McTavish", "u_m_m_willyfist", True)
    Public TylerDixon As New Person("Tyler Dixon", "ig_tylerdix", True)
    Public LazlowJones As New Person("Lazlow Jones", "ig_lazlow", True)
    Public IsiahFriedlander As New Person("Isiah Friedlander", "ig_drfriedlander", True)
    Public FabienLaRouche As New Person("Fabien LaRouche", "ig_fabien", True)
    Public PeterDreyfuss As New Person("Peter Dreyfuss", "ig_dreyfuss", True)
    Public KerryMcintosh As New Person("Kerry McIntosh", "ig_kerrymcintosh", True)
    Public JimmyBoston As New Person("Jimmy Boston", "ig_jimmyboston", True)
    Public MiltonMcIlroy As New Person("Milton McIlroy", "cs_milton", True)
    Public AnitaMendoza As New Person("Anita Mendoza", "csb_anita", True)
    Public HughHarrison As New Person("Hugh Harrison", "csb_hugh", True)
    Public ImranShinowa As New Person("Imran Shinowa", "csb_imran", True)
    Public SolomonRichards As New Person("Solomon Richards", "ig_solomon", True)
    Public AntonBeaudelaire As New Person("Anton Beaudelaire", "u_m_y_antonb", True)
    Public AbigailMathers As New Person("Abigail Mathers", "c_s_b_abigail", True)
    Public ChrisFormage As New Person("Cris Formage", "cs_chrisformage", True)
    'ADD EPSILON MEMBERS

    Public NonCeleb As New Person("", "", False)

End Module



'LOCATIONS

Public Enum LocationType
    Residential
    Entertainment
    Shopping
    Restaurant
    FastFood
    Bar
    Theater
    StripClub
    Sport
    HotelLS
    MotelLS
    MotelBC
    AirportDepart
    AirportArrive
    Religious
    Media
    School
    Office
    Factory
End Enum

Public Class Location
    Public Name As String = ""
    Public Type As LocationType
    Public Coords As New Vector3(0, 0, 0)
    Public PedStart As New Vector3(0, 0, 0)
    Public PedStartHDG As Integer
    Public PedEnd As New Vector3(0, 0, 0)
    Public isValidDestination As Boolean = False

    Public Sub New(n As String, coord As Vector3, t As LocationType, StartPos As Vector3, StartHeading As Integer, Optional ValidAsDestination As Boolean = True)
        Name = n
        Coords = coord
        Type = t
        PedStart = StartPos
        PedStartHDG = StartHeading
        isValidDestination = ValidAsDestination
        ListOfPlaces.Add(Me)
    End Sub
End Class

Public Module Places

    Public ListOfPlaces As New List(Of Location)

    Public lAirportD As New List(Of Location)
    Public lAirportA As New List(Of Location)
    Public lHotelLS As New List(Of Location)
    Public lMotelLS As New List(Of Location)
    Public lMotelBC As New List(Of Location)
    Public lResidential As New List(Of Location)
    Public lEntertainment As New List(Of Location)
    Public lBar As New List(Of Location)
    Public lShopping As New List(Of Location)
    Public lRestaurant As New List(Of Location)
    Public lFastFood As New List(Of Location)
    Public lReligious As New List(Of Location)
    Public lSport As New List(Of Location)
    Public lOffice As New List(Of Location)
    Public lStripClub As New List(Of Location)
    Public lEpsilon As New List(Of Location)
    Public lTheater As New List(Of Location)
    Public lSchool As New List(Of Location)

    'SCHOOL
    Public ULSA1 As New Location("ULSA Campus", New Vector3(-1572.412, 175.073, 57.622), LocationType.School, New Vector3(-1577.04, 183.68, 58.88), 219)
    Public ULSA2 As New Location("ULSA Campus", New Vector3(-1644.79, 141.821, 61.468), LocationType.School, New Vector3(-1649.18, 150.28, 62.17), 216)

    'RELIGIOUS
    Public EpsilonHQ As New Location("Epsilon HQ", New Vector3(-695.732, 39.476, 42.895), LocationType.Religious, New Vector3(-696.74, 44.1, 43.32), 179)
    Public HillValleyChurch As New Location("Hill Valley Church", New Vector3(-1688.557, -297.007, 51.34), LocationType.Religious, New Vector3(-1685.52, -292.62, 51.89), 190)
    Public RockfordHillsChurch As New Location("Rockford Hills Church", New Vector3(-761.49, -37.93, 36.97), LocationType.Religious, New Vector3(-766.56, -23.58, 41.08), 210)
    Public LittleSeoulChurch As New Location("Little Seoul Church", New Vector3(-768.87, -667.37, 29.15), LocationType.Religious, New Vector3(-765.65, -684.76, 30.09), 1)
    Public StBrigidBaptist As New Location("St Brigid Baptist Church", New Vector3(-340.22, 6160.46, 31.01), LocationType.Religious, New Vector3(-331.65, 6150.4, 32.31), 85)

    'SPORT
    Public Golfcourse As New Location("Los Santos Country Club", New Vector3(-1378.862, 45.125, 53.367), LocationType.Sport, New Vector3(-1367.97, 56.55, 53.83), 92)
    Public TennisRichman As New Location("Richman Hotel Tennis Courts", New Vector3(-1256.139, 396.519, 74.882), LocationType.Sport, New Vector3(-1255.72, 371.67, 75.87), 51) With {.PedEnd = New Vector3(-1230.9, 365.97, 79.98)}
    Public TennisULSA As New Location("ULSA Training Center Tennis Courts", New Vector3(-1654.964, 291.968, 59.93), LocationType.Sport, New Vector3(-1639.79, 275.91, 59.55), 244)
    Public DeckerPark As New Location("Decker Park", New Vector3(-864.56, -679.59, 27), LocationType.Sport, New Vector3(-893.83, -707.27, 19.82), 340)
    Public PBCC As New Location("Pacific Bluffs Country Club", New Vector3(-3016.76, 85.64, 11.2), LocationType.Sport, New Vector3(-3023.85, 80.96, 11.61), 317)
    Public RatonCanyonTrails As New Location("Raton Canyon Trails", New Vector3(-1511.41, 4971.09, 61.95), LocationType.Sport, New Vector3(-1492.77, 4968.99, 63.93), 75) With {.PedEnd = New Vector3(-1573.99, 4848.2, 60.58)}
    Public sDPBeachS As New Location("South Del Perro Beach", New Vector3(-1457.92, -963.99, 6.75), LocationType.Sport, New Vector3(-1463.03, -979.96, 6.91), 181)
    Public sDPBeachC As New Location("Central Del Perro Beach", New Vector3(-1725.3, -733.1, 9.9), LocationType.Sport, New Vector3(-1730.3, -739.3, 9.95), 230)
    Public sVBeachN1 As New Location("North Vespucci Beach", New Vector3(-1400.62, -1028.97, 3.88), LocationType.Sport, New Vector3(-1413.8, -1046.67, 4.62), 135)
    Public sVBeachN2 As New Location("North Vespucci Beach", New Vector3(-1345.2, -1205.1, 4.2), LocationType.Sport, New Vector3(-1367.04, -1196.21, 4.45), 212)
    Public sDPBeachN As New Location("North Del Perro Beach", New Vector3(-1862.15, -616.73, 10.76), LocationType.Sport, New Vector3(-1868.61, -631.09, 11.09), 130)
    Public MirrorPark As New Location("Mirror Park", New Vector3(1040.04, -531.29, 60.78), LocationType.Sport, New Vector3(1046.1, -535.54, 61.03), 207)
    Public PuertaDelSol As New Location("Puerta Del Sol", New Vector3(-816.06, -133.44, 4.62), LocationType.Sport, New Vector3(-816.41, -1346.46, 5.15), 48)
    Public KoiSpa As New Location("Koi Retreat & Spa", New Vector3(-1047.08, -1467.7, 4.6), LocationType.Sport, New Vector3(-1040.63, -1474.9, 5.6), 53)
    Public ParkSheld As New Location("N Sheldon Ave Park", New Vector3(-807.1, 826.7, 202.6), LocationType.Sport, New Vector3(-805.7, 840.9, 203.5), 359)

    'FAST FOOD
    Public DPPUnA As New Location("Up-n-Atom, Del Perro Plaza", New Vector3(-1529.8, -444.44, 34.73), LocationType.FastFood, New Vector3(-1552.48, -440, 40.52), 229)
    Public DPPTaco As New Location("Taco Bomb, Del Perro Plaza", New Vector3(-1529.8, -444.44, 34.73), LocationType.FastFood, New Vector3(-1552.48, -440, 40.52), 229)
    Public DPPBean As New Location("Bean Machine, Del Perro Plaza", New Vector3(-1529.8, -444.44, 34.73), LocationType.FastFood, New Vector3(-1548.81, -435.8, 35.89), 240)
    Public DPPChihu As New Location("Chihuahuha Hotdogs, Del Perro Plaza", New Vector3(-1529.8, -444.44, 34.73), LocationType.FastFood, New Vector3(-1534.29, -421.96, 35.59), 211)
    Public DPPBite As New Location("Bite, Del Perro Plaza", New Vector3(-1529.8, -444.44, 34.73), LocationType.FastFood, New Vector3(-1540.61, -428.96, 35.59), 254)
    Public DPPWig As New Location("Wigwam Burger, Del Perro Plaza", New Vector3(-1529.8, -444.44, 34.73), LocationType.FastFood, New Vector3(-1535.02, -451.73, 35.88), 308)
    Public TacoLibre As New Location("Taco Libre", New Vector3(-1181.24, -1270.67, 5.57), LocationType.FastFood, New Vector3(-1169.86, -1264.5, 6.6), 152)
    Public UnAVine As New Location("Up-n-Atom, Vinewood", New Vector3(70.47, 258.4, 108.45), LocationType.FastFood, New Vector3(78.34, 273.76, 110.21), 198)
    Public WigVesp As New Location("Wigwam Burger, Vespucci", New Vector3(-850.75, -1149.78, 5.6), LocationType.FastFood, New Vector3(-861.85, -1142.63, 6.99), 234)
    Public BeanLitSeo As New Location("Bean Machine, Little Seoul", New Vector3(-826.09, -641.22, 26.91), LocationType.FastFood, New Vector3(-839, -609.28, 29.03), 146)
    Public SHoLitSeo As New Location("S. Ho Korean Noodle House, Little Seoul", New Vector3(-826.09, -641.22, 26.91), LocationType.FastFood, New Vector3(-798.27, -635.43, 29.03), 100)
    Public NoodlLS As New Location("Noodle Exchange, Legion Square", New Vector3(260.26, -970.32, 28.7), LocationType.FastFood, New Vector3(272.12, -964.79, 29.3), 42)
    Public CoolBeansLS As New Location("Cool Beans, Legion Square", New Vector3(260.26, -970.32, 28.7), LocationType.FastFood, New Vector3(263.08, -981.74, 29.36), 86)
    Public CoolBeansMP As New Location("Cool Beans, Mirror Park", New Vector3(1195.04, -403.86, 67.56), LocationType.FastFood, New Vector3(1181.57, -393.69, 68.02), 227)
    Public Hornys As New Location("Horny's, Mirror Park", New Vector3(1239.12, -376.58, 68.6), LocationType.FastFood, New Vector3(1241.25, -367.15, 69.08), 176)
    Public BSDP As New Location("Burger Shot, Del Perro", New Vector3(-1205, -878.4, 12.8), LocationType.FastFood, New Vector3(-1198, -883.9, 13.8), 33)
    Public CocoCafe As New Location("Coconut Cafe, Vespucci", New Vector3(-1104.8, -1451.3, 4.6), LocationType.FastFood, New Vector3(-1110.8, -1453.1, 5.1), 252)
    Public IceMaiden As New Location("Icemaiden, Vespucci", New Vector3(-1173.7, -1428.4, 4), LocationType.FastFood, New Vector3(-1171.9, -1434.6, 4.4), 28)
    Public MusclePeach As New Location("Muscle Peach Cafe, Vespucci", New Vector3(-1187.5, -1528.6, 4), LocationType.FastFood, New Vector3(-1186.9, -1533.7, 4.4), 5)

    'RESTAURANT
    Public LaSpada As New Location("La Spada", New Vector3(-1046.724, -1398.146, 4.949), LocationType.Restaurant, New Vector3(-1038.01, -1396.84, 5.55), 84)
    Public ChebsEaterie As New Location("Chebs Eaterie", New Vector3(-730.21, -330.45, 35), LocationType.Restaurant, New Vector3(-735.26, -319.63, 36.22), 187)
    Public CafeRedemption As New Location("Cafe Redemption", New Vector3(-641.08, -308.14, 34.21), LocationType.Restaurant, New Vector3(-634.26, -302.17, 35.06), 131)
    Public LastTrain As New Location("Last Train In Los Santos", New Vector3(-364.02, 251.64, 83.9), LocationType.Restaurant, New Vector3(-369.1, 267.09, 84.84), 186)
    Public AlDentV As New Location("Al Dente's, Vespucci", New Vector3(-1184.04, -1419.9, 3.98), LocationType.Restaurant, New Vector3(-1186.5, -1413.4, 4.4), 199)
    Public PrawnViv As New Location("Prawn Vivant, Vespucci", New Vector3(-1227.5, -1096.9, 7.6), LocationType.Restaurant, New Vector3(-1221.8, -1096.2, 8.1), 107)

    'BAR
    Public PipelineInn As New Location("Pipeline Inn", New Vector3(-2182.395, -391.984, 12.83), LocationType.Bar, New Vector3(-2192.54, -389.54, 13.47), 249)
    Public EclipseLounge As New Location("Eclipse Lounge", New Vector3(-83, 246.52, 99.77), LocationType.Bar, New Vector3(-84.96, 235.74, 100.56), 2)
    Public MojitoInn As New Location("Mojito Inn, Paleto Bay", New Vector3(-130.08, 6396.05, 30.88), LocationType.Bar, New Vector3(-121.04, 6394.28, 31.49), 82)
    Public Henhouse As New Location("The Hen House, Paleto Bay", New Vector3(-295.27, 6248.5, 30.82), LocationType.Bar, New Vector3(-295.15, 6259.08, 31.49), 174)
    Public BayviewLodge As New Location("Bayview Lodge, Paleto Bay", New Vector3(-700, 5816.39, 16.68), LocationType.Bar, New Vector3(-697.98, 5802.34, 17.33), 54)
    Public Sightings As New Location("Sightings Bar & Restaurant", New Vector3(-865.79, -2543.2, 13.33), LocationType.Bar, New Vector3(-886.79, -2536.09, 14.55), 240)
    Public Tequi As New Location("Tequi-La-La", New Vector3(-564.86, 267.92, 82.43), LocationType.Bar, New Vector3(-567.94, 274.83, 83.02), 194)
    Public DungeonCrawler As New Location("Dungeon Crawler", New Vector3(-259.71, 252.11, 90.59), LocationType.Bar, New Vector3(-264.45, 245.73, 90.77), 344)
    Public Cockatoos As New Location("Cockatoos Nightclub", New Vector3(-421.99, -34.9, 45.75), LocationType.Bar, New Vector3(-430.12, -24.4, 46.23), 274)
    Public PenalCol As New Location("Penal Colony Nightclub", New Vector3(-536.57, -64.83, 40.7), LocationType.Bar, New Vector3(-531.01, -62.84, 41.02), 139)
    Public RobsChum As New Location("Rob's Liquor, Chumash", New Vector3(-2981.17, 389.71, 14.14), LocationType.Bar, New Vector3(-2968.54, 390.26, 15.04), 292)
    Public RobsVes As New Location("Rob's Liquor, Vespucci", New Vector3(-1230.02, -896.64, 11.43), LocationType.Bar, New Vector3(-1223.81, -906.32, 12.33), 229)
    Public RobsDP As New Location("Rob's Liquor, Del Perro", New Vector3(-1499.01, -394.84, 38.71), LocationType.Bar, New Vector3(-1487.55, -379.76, 40.16), 330)
    Public Bahama As New Location("Bahama Mama's West", New Vector3(-1394.2, -581.62, 29.47), LocationType.Bar, New Vector3(-1389.47, -586.25, 30.26), 10)
    Public Chaps As New Location("Chaps Nightclub", New Vector3(-475.51, -101.19, 38.35), LocationType.Bar, New Vector3(-473.49, -94.59, 39.28), 164)
    Public Singletons As New Location("Singletons Bar", New Vector3(233.21, 301.46, 105.17), LocationType.Bar, New Vector3(221.6, 307.5, 105.57), 194)
    Public Clappers As New Location("Clappers", New Vector3(405.86, 131.85, 101.36), LocationType.Bar, New Vector3(412.27, 150.72, 103.21), 161)
    Public MirrParTav As New Location("Mirror Park Tavern", New Vector3(1209.94, -415.29, 67.26), LocationType.Bar, New Vector3(1217.94, -416.8, 67.78), 78)
    Public RobsMurr As New Location("Rob's Liquor, Murietta Heights", New Vector3(1149.2, -980.4, 45.7), LocationType.Bar, New Vector3(1136.3, -979.4, 46.4), 25)

    'SHOPPING
    Public BobMulet As New Location("Bob Mulet Hair & Beauty", New Vector3(-830.47, -190.49, 36.74), LocationType.Shopping, New Vector3(-812.96, -184.69, 37.57), 36)
    Public PonsonPD As New Location("Ponsonbys Portola Drive", New Vector3(-722.99, -162.11, 36.22), LocationType.Shopping, New Vector3(-711.15, -152.5, 37.42), 280)
    Public ChumSU As New Location("SubUrban, Chumash Plaza", New Vector3(-3153.66, 1062.2, 20.25), LocationType.Shopping, New Vector3(-3177.78, 1037.39, 20.86), 341)
    Public ChumInk As New Location("Ink Inc, Chumash Plaza", New Vector3(-3153.66, 1062.2, 20.25), LocationType.Shopping, New Vector3(-3169.05, 1076.69, 20.83), 166)
    Public ChumAmmu As New Location("AmmuNation, Chumash Plaza", New Vector3(-3153.66, 1062.2, 20.25), LocationType.Shopping, New Vector3(-3171.64, 1087.57, 20.84), 44)
    Public ChumNelsons As New Location("Nelson's General Store, Chumash Plaza", New Vector3(-3153.66, 1062.2, 20.25), LocationType.Shopping, New Vector3(-3152.25, 1110.59, 20.87), 232)
    Public PonsonRP As New Location("Ponsonbys Rockford Plaza", New Vector3(-148.33, -308.77, 37.83), LocationType.Shopping, New Vector3(-168.03, -299.35, 39.73), 306)
    Public JonnyTung As New Location("Jonny Tung", New Vector3(-611.62, -316.21, 34), LocationType.Shopping, New Vector3(-620.14, -309.45, 34.82), 162)
    Public HelgaKrepp As New Location("Helga Kreppsohle", New Vector3(-647.68, -297.63, 34.51), LocationType.Shopping, New Vector3(-638.52, -293.33, 35.3), 109)
    Public Dalique As New Location("Dalique", New Vector3(-647.68, -297.63, 34.51), LocationType.Shopping, New Vector3(-643.21, -285.69, 35.5), 117)
    Public WinfreyCasti As New Location("Winfrey Castiglione", New Vector3(-659.29, -276.9, 35.02), LocationType.Shopping, New Vector3(-649.25, -276.35, 35.73), 95)
    Public LittlePortola As New Location("Little Portola", New Vector3(-676.69, -215.97, 36.31), LocationType.Shopping, New Vector3(-662.16, -227.05, 37.47), 53)
    Public ArirangPlaza As New Location("Arirang Plaza", New Vector3(-688.49, -826.51, 23.15), LocationType.Shopping, New Vector3(-690.75, -813.6, 23.93), 179)
    Public SimmetAlley As New Location("Simmet Alley", New Vector3(455.45, -820.15, 27.07), LocationType.Shopping, New Vector3(460.21, -794.62, 27.36), 89)
    Public Krapea As New Location("Krapea, Textile City", New Vector3(330.75, -772.94, 28.68), LocationType.Shopping, New Vector3(337.8, -777.29, 29.27), 69)
    Public Chu247 As New Location("24-7, Chumash Family Pier", New Vector3(-3235.04, 1005.12, 11.85), LocationType.Shopping, New Vector3(-3243.35, 1002.05, 12.83), 173)
    Public ChuHang As New Location("Hang Ten, Chumash", New Vector3(-2977.1, 433.89, 14.33), LocationType.Shopping, New Vector3(-2965.07, 432.85, 15.28), 94)
    Public ChuTide As New Location("Tidemarks, Chumash", New Vector3(-2976.36, 457.16, 14.43), LocationType.Shopping, New Vector3(-2963.84, 454.82, 15.32), 91)
    Public PonsonMW As New Location("Ponsonbys Morningwood", New Vector3(-1456.74, -225.29, 48.34), LocationType.Shopping, New Vector3(-1451.61, -241.21, 49.81), 321)
    Public MexMark As New Location("Mexican Market", New Vector3(402.8, -382.98, 46.06), LocationType.Shopping, New Vector3(392.42, -368.65, 46.81), 217)
    Public DidierPH As New Location("Didier Sachs, Pillbox Hill", New Vector3(-226.51, -962.32, 28.45), LocationType.Shopping, New Vector3(-248.98, -954.6, 31.22), 260)
    Public HawSn As New Location("Hawaiian Snow, Alta", New Vector3(278.99, -228.33, 53.27), LocationType.Shopping, New Vector3(281.56, -220.32, 53.98), 147)
    Public WhWid As New Location("White Widow, Alta", New Vector3(211.96, -230.93, 53.13), LocationType.Shopping, New Vector3(202.15, -239.65, 53.97), 308)
    Public PillRH As New Location("PillPharm, Rockford Hills", New Vector3(-382.72, -400.09, 30.95), LocationType.Shopping, New Vector3(-389.93, -421.66, 31.62), 343)
    Public VPAmmu As New Location("AmmuNation, Vinewood Plaza", New Vector3(237.52, -44.36, 69.28), LocationType.Shopping, New Vector3(251.34, -49.2, 69.94), 45)
    Public Freds As New Location("Fred's Store", New Vector3(337.57, 132.14, 102.6), LocationType.Shopping, New Vector3(333.23, 119.31, 104.31), 310)
    Public BlazingTat As New Location("Blazing Tattoo, Vinewood", New Vector3(316.39, 165.29, 103.28), LocationType.Shopping, New Vector3(320.9, 183.07, 103.59), 221)
    Public DavisMM As New Location("Davis Mega Mall", New Vector3(68.09, -1707.57, 28.67), LocationType.Shopping, New Vector3(61.38, -1728.26, 29.53), 46)
    Public VBSidewMark As New Location("Vespucci Beach Sidewalk Market", New Vector3(-1208.53, -1444.11, 3.9), LocationType.Shopping, New Vector3(-1237.16, -1468.65, 4.29), 126)
    Public ThePit As New Location("The Pit", New Vector3(-1163.91, -1415.34, 4.38), LocationType.Shopping, New Vector3(-1155.54, -1426.46, 4.95), 319)
    Public VespMall As New Location("Vespucci Mall", New Vector3(-803.1, -1095.8, 10.4), LocationType.Shopping, New Vector3(-824, -1084.3, 11.1), 256)
    Public Harmony247 As New Location("24-7 Supermarket, Harmony", New Vector3(542.8, 2680, 42), LocationType.Shopping, New Vector3(547.7, 2669.5, 42.2), 273)
    Public Gabrielas As New Location("Gabriela's Market, Mirror Park", New Vector3(1175, -280.3, 68.5), LocationType.Shopping, New Vector3(1168.8, -290.9, 69), 329)
    Public Leopolds As New Location("Leopolds Rockford Hills", New Vector3(-692.9, -372.1, 33.7), LocationType.Shopping, New Vector3(-697.8, -379.8, 34.5), 334)
    Public EchorockPl As New Location("Echorock Shopping Plaza", New Vector3(94.3, -185.8, 54.3), LocationType.Shopping, New Vector3(106.5, -206, 54.6), 37)

    'ENTERTAINMENT
    Public DelPerroPier As New Location("Del Perro Pier", New Vector3(-1624.56, -1008.23, 12.4), LocationType.Entertainment, New Vector3(-1638, -1012.97, 13.12), 346) With {.PedEnd = New Vector3(-1841.98, -1213.19, 13.02)}
    Public Tramway As New Location("Pala Springs Aerial Tramway", New Vector3(-771.53, 5582.98, 33.01), LocationType.Entertainment, New Vector3(-755.66, 5583.63, 36.71), 91) With {.PedEnd = New Vector3(-745.23, 5594.77, 41.65)}
    Public LSGC As New Location("Los Santos Gun Club", New Vector3(16.86, -1125.85, 29.3), LocationType.Entertainment, New Vector3(20.24, -1107.24, 29.8), 173)
    Public MazeBankArena As New Location("Maze Bank Arena", New Vector3(-235.91, -1863.7, 28.03), LocationType.Entertainment, New Vector3(-260.4, -1897.91, 27.76), 8)
    Public DPBeachS As New Location("South Del Perro Beach", New Vector3(-1457.92, -963.99, 6.75), LocationType.Entertainment, New Vector3(-1463.03, -979.96, 6.91), 181)
    Public VBeachN1 As New Location("North Vespucci Beach", New Vector3(-1400.62, -1028.97, 3.88), LocationType.Entertainment, New Vector3(-1413.8, -1046.67, 4.62), 135)
    Public VBeachN2 As New Location("North Vespucci Beach", New Vector3(-1345.2, -1205.1, 4.2), LocationType.Entertainment, New Vector3(-1367.04, -1196.21, 4.45), 212)
    Public SplitSides As New Location("Split Sides West", New Vector3(-429.43, 252.64, 82.51), LocationType.Entertainment, New Vector3(-423.71, 259.76, 83.1), 167)
    Public ChuFamPier As New Location("Chumash Family Pier", New Vector3(-3235.25, 968.84, 12.59), LocationType.Entertainment, New Vector3(-3239.96, 971.7, 12.7), 90) With {.PedEnd = New Vector3(-3426.4, 967.81, 8.35)}
    Public Kortz As New Location("Kortz Center", New Vector3(-2296.4, 376.32, 173.75), LocationType.Entertainment, New Vector3(-2288.4, 353.93, 174.6), 3)
    Public Galileo As New Location("Galileo Observatory", New Vector3(-411.51, 1174.21, 324.92), LocationType.Entertainment, New Vector3(-415.25, 1166.59, 325.85), 340)
    Public BetsyPav As New Location("Betsy O'Neil Pavilion", New Vector3(-548.26, -648.62, 32.42), LocationType.Entertainment, New Vector3(-555.76, -620.95, 34.68), 183)
    Public SAGOMA As New Location("S.A. Gallery of Modern Art", New Vector3(-424.08, 13.09, 45.75), LocationType.Entertainment, New Vector3(-424.55, 22.97, 46.26), 178)
    Public Casino As New Location("Be Lucky Casino", New Vector3(919.58, 48.24, 90.39), LocationType.Entertainment, New Vector3(929.17, 42.99, 81.09), 59)
    Public DPPierNorth As New Location("Del Perro Pier, North Entrance", New Vector3(-1650.6, -951.3, 7.4), LocationType.Entertainment, New Vector3(-1664.32, -967.68, 7.63), 321) With {.PedEnd = New Vector3(-1673.9, -997.3, 7.4)}
    Public Bishops As New Location("Bishop's WTF", New Vector3(59.8, 233.7, 108.8), LocationType.Entertainment, New Vector3(58.5, 224.6, 109.3), 345)

    'THEATER
    Public LosSantosTheater As New Location("Los Santos Theater", New Vector3(345.33, -867.2, 28.72), LocationType.Theater, New Vector3(353.7, -874.09, 29.29), 8)
    Public TenCentTheater As New Location("Ten Cent Theater", New Vector3(401.16, -711.92, 28.7), LocationType.Theater, New Vector3(394.68, -710.04, 29.28), 254)
    Public TivoliTheater As New Location("Tivoli Theater", New Vector3(-1430.66, -193.99, 46.59), LocationType.Theater, New Vector3(-1423.98, -213.35, 46.5), 359)
    Public MorningwoodTheater As New Location("Morningwood Theater", New Vector3(-1389.53, -190.44, 46.12), LocationType.Theater, New Vector3(-1372.27, -173.32, 47.47), 84)
    Public Whirly As New Location("Whirlygig Theater", New Vector3(306.1, 145.38, 103.31), LocationType.Theater, New Vector3(303.21, 136.62, 103.81), 337)
    Public Oriental As New Location("Oriental Theater", New Vector3(292.32, 176, 103.7), LocationType.Theater, New Vector3(292.3, 192.13, 104.37), 195)
    Public Doppler As New Location("Doppler Theater", New Vector3(330.98, 161.02, 102.94), LocationType.Theater, New Vector3(337.21, 177.19, 103.12), 344)

    'S CLUB
    Public StripHornbills As New Location("Hornbill's", New Vector3(-380.469, 230.008, 83.622), LocationType.StripClub, New Vector3(-386.78, 220.33, 83.79), 6)
    Public StripVanUni As New Location("Vanilla Unicorn", New Vector3(133.93, -1307.91, 28.28), LocationType.StripClub, New Vector3(127.26, -1289.09, 29.28), 206)

    'WORK
    Public LombankLittleSeoul As New Location("Lombank, Little Seoul", New Vector3(-688.22, -648.09, 30.37), LocationType.Office, New Vector3(-687.1, -617.62, 31.56), 157)
    Public AuguryIns As New Location("Augury Insurance", New Vector3(-289.45, -412.25, 29.25), LocationType.Office, New Vector3(-296.18, -424.89, 30.24), 325)
    Public IAA As New Location("International Affairs Agency", New Vector3(106.08, -611.18, 43.63), LocationType.Office, New Vector3(117.54, -622.52, 44.24), 53)
    Public FIB As New Location("Federal Investigation Bureau", New Vector3(63.71, -727.4, 43.63), LocationType.Office, New Vector3(102.02, -742.66, 45.75), 103)
    Public UD As New Location("Union Depository", New Vector3(-8.13, -741.36, 43.74), LocationType.Office, New Vector3(5.28, -709.38, 45.97), 184)
    Public MB As New Location("Maze Bank Tower", New Vector3(-50.06, -785.19, 43.75), LocationType.Office, New Vector3(-66.16, -800.06, 44.23), 334)
    Public DG As New Location("Daily Globe International", New Vector3(-300.64, -620.04, 33), LocationType.Office, New Vector3(-317.41, -610.01, 33.56), 250)
    Public Weazel As New Location("Weazel News Studio", New Vector3(-621.96, -930.59, 21.84), LocationType.Office, New Vector3(-600.64, -929.9, 23.86), 95)
    Public NooseLSIA As New Location("N.O.O.S.E. LSIA", New Vector3(-880.37, -2419.36, 13.36), LocationType.Office, New Vector3(-894.21, -2401.18, 14.02), 191)
    Public BilgecoLSIA As New Location("Bilgeco Shipping Services", New Vector3(-1006.02, -2113.84, 11.37), LocationType.Office, New Vector3(-1024.47, -2127.24, 13.16), 307)
    Public LSCustLSIA As New Location("Los Santos Customs, LSIA", New Vector3(-1132.49, -1989.56, 12.67), LocationType.Office, New Vector3(-1141.05, -1991.34, 13.16), 276)
    Public BurtonHealth As New Location("Burton Health Center", New Vector3(-423.94, -67.88, 42.29), LocationType.Office, New Vector3(-431.83, -60.78, 43), 274)
    Public ChumFleeca As New Location("Fleeca Bank, Chumash", New Vector3(-2974.37, 483.18, 14.55), LocationType.Office, New Vector3(-2967.19, 482.39, 15.69), 99)
    Public CityHallDP As New Location("Del Perro City Hall", New Vector3(-1272.96, -560.89, 29.14), LocationType.Office, New Vector3(-1285.19, -566.24, 31.71), 307)
    Public MazeOfficeDP As New Location("Maze Bank Office, Del Perro", New Vector3(-1401.03, -514.78, 31.03), LocationType.Office, New Vector3(-1382.44, -502.77, 33.16), 179)
    Public LiveInv As New Location("Live Invader HQ", New Vector3(-1076.87, -265.67, 36.96), LocationType.Office, New Vector3(-1084.55, -262.85, 37.76), 238)
    Public PenrisDT1 As New Location("Penris Tower, Downtown", New Vector3(148.62, -583.3, 43.21), LocationType.Office, New Vector3(155.67, -566.55, 43.89), 122)
    Public PenrisDT2 As New Location("Penris Tower, Downtown", New Vector3(252.96, -569.05, 42.45), LocationType.Office, New Vector3(217.36, -564.97, 43.87), 297)
    Public CityHallLS As New Location("Los Santos City Hall", New Vector3(257.4, -377.35, 43.84), LocationType.Office, New Vector3(251.39, -389.63, 45.4), 331) With {.PedEnd = New Vector3(235.62, -411.8, 48.11)}
    Public LombankDT As New Location("Lombank Tower, Downtown", New Vector3(0.03, -947.8, 28.53), LocationType.Office, New Vector3(6.34, -934.49, 29.91), 120)
    Public CityHallRH As New Location("Rockford Hills City Hall", New Vector3(-515.6, -265, 34.9), LocationType.Office, New Vector3(-519.76, -255.26, 35.65), 228) With {.PedEnd = New Vector3(-544.91, -205.23, 38.22)}
    Public Slaughter3 As New Location("Slaughter, Slaughter & Slaughter", New Vector3(-243.19, -708.09, 33.06), LocationType.Office, New Vector3(-271.49, -703.8, 38.28), 272)
    Public Schlongberg As New Location("Schlongberg Sachs", New Vector3(-232.97, -722.25, 33.06), LocationType.Office, New Vector3(-213.97, -728.8, 33.55), 82)
    Public Arcadius As New Location("Arcadius Business Center", New Vector3(-108.01, -613.95, 35.66), LocationType.Office, New Vector3(-116.6, -605, 36.28), 251)
    Public GalileoHouse As New Location("Galileo House", New Vector3(389.24, -82.62, 67.32), LocationType.Office, New Vector3(389.43, -75.65, 68.18), 164)
    Public BadgerB As New Location("Badger Building", New Vector3(460.49, -138.47, 61.42), LocationType.Office, New Vector3(478.25, -107.44, 63.16), 144)
    Public PacStanBank As New Location("Pacific Standard Bank", New Vector3(225.61, 200.76, 104.96), LocationType.Office, New Vector3(239.99, 219.95, 106.29), 308)
    Public Wenger1 As New Location("Wenger Institute", New Vector3(-294.94, -279.51, 30.61), LocationType.Office, New Vector3(-309.69, -279.12, 31.72), 265)
    Public Wenger2 As New Location("Wenger Institute", New Vector3(-383.61, -237.61, 35.17), LocationType.Office, New Vector3(-369.15, -240.52, 36.08), 61)
    Public Vesp707 As New Location("707 Vespucci Blvd", New Vector3(-274.7, -834.1, 31.2), LocationType.Office, New Vector3(-262.4, -837.6, 31.5), 129)
    Public RebelRad As New Location("Rebel Radio Studio", New Vector3(741.6, 2523.4, 72.8), LocationType.Office, New Vector3(733, 2523.7, 73.2), 255)
    Public WeazelPlaza As New Location("Weazel Plaza", New Vector3(-860.5, 389, 39), LocationType.Office, New Vector3(-858.9, -407.8, 36.6), 203)

    'HOTEL
    Public HotelRichman As New Location("Richman Hotel", New Vector3(-1285.498, 294.565, 64.368), LocationType.HotelLS, New Vector3(-1274.5, 313.97, 65.51), 151)
    Public HotelVenetian As New Location("The Venetian Hotel", New Vector3(-1330.82, -1095.89, 6.37), LocationType.HotelLS, New Vector3(-1342.88, -1080.84, 6.94), 255)
    Public HotelViceroy As New Location("The Viceroy Hotel", New Vector3(-828.04, -1218.07, 6.46), LocationType.HotelLS, New Vector3(-821.72, -1221.23, 7.33), 60)
    Public HotelRockDors As New Location("The Rockford Dorset Hotel", New Vector3(-569.89, -384.24, 34.19), LocationType.HotelLS, New Vector3(-570.79, -394.13, 35.07), 350)
    Public HotelVCPacificBluffs As New Location("Von Crastenburg Hotel, Pacific Bluffs", New Vector3(-1862.575, -352.879, 48.752), LocationType.HotelLS, New Vector3(-1859.5, -347.92, 49.84), 157)
    Public HotelBannerDP As New Location("Banner Hotel, Del Perro", New Vector3(-1668.53, -542.07, 34.31), LocationType.HotelLS, New Vector3(-1662.99, -535.5, 35.33), 152)
    Public HotelVCRock1 As New Location("Von Crastenburg Hotel, Richman", New Vector3(-1208.148, -128.829, 40.71), LocationType.HotelLS, New Vector3(-1239.6, -156.26, 40.41), 62)
    Public HotelVCRock2 As New Location("Von Crastenburg Hotel, Richman", New Vector3(-1228.485, -193.734, 38.8), LocationType.HotelLS, New Vector3(-1239.6, -156.26, 40.41), 62)
    Public HotelEmissary As New Location("Emissary Hotel", New Vector3(116.09, -935.88, 28.94), LocationType.HotelLS, New Vector3(106.31, -933.52, 29.79), 254)
    Public HotelAlesandro As New Location("Alesandro Hotel", New Vector3(318.07, -732.85, 28.73), LocationType.HotelLS, New Vector3(309.89, -728.62, 29.32), 251)
    Public HotelVCLSIA As New Location("Von Crastenburg Hotel, LSIA", New Vector3(-887.55, -2187.61, 7.81), LocationType.HotelLS, New Vector3(-878.67, -2179.05, 9.81), 134)
    Public HotelVCLSIA2 As New Location("Von Crastenburg Hotel, LSIA", New Vector3(-882.35, -2107.39, 8.14), LocationType.HotelLS, New Vector3(-878.67, -2179.05, 9.81), 134)
    Public HotelOpium As New Location("Opium Nights Hotel, LSIA", New Vector3(-689.92, -2287.82, 12.87), LocationType.HotelLS, New Vector3(-702.13, -2276.6, 13.46), 229)
    Public HotelOpium2 As New Location("Opium Nights Hotel, LSIA", New Vector3(-754.43, -2292.86, 12.14), LocationType.HotelLS, New Vector3(-737.53, -2277.44, 13.44), 133)
    Public HotelBannerPH As New Location("Banner Hotel, Pillbox Hill", New Vector3(-278.28, -1065.08, 25.04), LocationType.Entertainment, New Vector3(-286.06, -1061.29, 27.21), 253)
    Public HotelGeneric As New Location("The Generic Hotel", New Vector3(-479.44, 225.87, 82.63), LocationType.Entertainment, New Vector3(-482.92, 219.25, 83.7), 341)
    Public HotelPegasusConc As New Location("Pegasus Concierge Hotel", New Vector3(-310.2, 226.61, 87.43), LocationType.Entertainment, New Vector3(-310.84, 222.29, 87.93), 12)
    Public HotelGentry As New Location("Gentry Manor Hotel", New Vector3(-62.57, 329.18, 110.3), LocationType.Entertainment, New Vector3(-53.93, 356.92, 113.06), 181)
    Public HotelVineGar As New Location("Vinewood Gardens Hotel", New Vector3(322.17, -87.74, 68.19), LocationType.Entertainment, New Vector3(328.71, -70.77, 72.25), 161)
    Public HotelVCVine As New Location("Von Crastenburg Hotel, Vinewood", New Vector3(437.14, 221.31, 102.77), LocationType.Entertainment, New Vector3(435.53, 215.57, 103.17), 340)

    'MOTEL
    Public PerreraBeach As New Location("Perrera Beach Motel", New Vector3(-1480.4, -669.76, 28.23), LocationType.MotelLS, New Vector3(-1478.68, -649.89, 29.58), 162) With {.PedEnd = New Vector3(-1479.64, -674.43, 29.04)}
    Public DreamView As New Location("Dream View Motel, Paleto Bay", New Vector3(-94.06, 6310.33, 31.02), LocationType.MotelBC, New Vector3(-106.33, 6315.21, 31.49), 212)
    Public CrownJewels As New Location("Crown Jewels Motel", New Vector3(-1300.2, -922.46, 10.55), LocationType.MotelLS, New Vector3(-1308.91, -930.84, 13.36), 313)
    Public PinkCage As New Location("Pink Cage Motel", New Vector3(314.31, -244.63, 53.22), LocationType.MotelLS, New Vector3(313.91, -227.21, 54.02), 229)
    Public AltaMotel As New Location("Alta Motel", New Vector3(66.07, -283.71, 46.68), LocationType.MotelLS, New Vector3(62.95, -255.06, 48.19), 84)
    Public EasternMotel As New Location("Eastern Motel, Harmony", New Vector3(324.1, 2626.7, 44.2), LocationType.MotelBC, New Vector3(318.6, 2623, 44.5), 312)

    'AIRPORT
    Public LSIA1Depart = New Location("LSIA Terminal 1 Departures", New Vector3(-1016.76, -2477.951, 19.596), LocationType.AirportDepart, New Vector3(-1029.35, -2486.58, 20.17), 253)
    Public LSIA4Depart As New Location("LSIA Terminal 4 Departures", New Vector3(-1033.549, -2730.294, 19.583), LocationType.AirportDepart, New Vector3(-1037.78, -2748.22, 21.36), 8)
    Public LSIA1Arrive = New Location("LSIA Terminal 1 Arrivals", New Vector3(-1016, -2482.257, 13.155), LocationType.AirportArrive, New Vector3(-1023.33, -2479.52, 13.94), 238, False)
    Public LSIA2Arrive = New Location("LSIA Terminal 2 Arrivals", New Vector3(-1047.546, -2536.564, 13.148), LocationType.AirportArrive, New Vector3(-1061.04, -2544.39, 13.94), 356, False)
    Public LSIA3Arrive = New Location("LSIA Terminal 3 Arrivals", New Vector3(-1082.547, -2598.047, 13.169), LocationType.AirportArrive, New Vector3(-1087.61, -2588.75, 13.88), 281, False)
    Public LSIA4Arrive As New Location("LSIA Terminal 4 Arrivals", New Vector3(-1018.835, -2731.926, 13.17), LocationType.AirportArrive, New Vector3(-1043.18, -2737.67, 13.86), 328, False)

    'RESIDENTIAL
    Public NRD1018 As New Location("1018 North Rockford Dr", New Vector3(-1962.174, 617.408, 120.52), LocationType.Residential, New Vector3(-1974.471, 630.919, 122.54), 250)
    Public NRD1012 As New Location("1012 North Rockford Dr", New Vector3(-2004.645, 482.538, 105.446), LocationType.Residential, New Vector3(-2014.892, 499.58, 107.17), 253)
    Public NRD1016 As New Location("1016 North Rockford Dr", New Vector3(-1981.123, 599.796, 117.889), LocationType.Residential, New Vector3(-1995.21, 590.85, 117.9), 256)
    Public NRD1010 As New Location("1010 North Rockford Dr", New Vector3(-1998.424, 456.349, 101.974), LocationType.Residential, New Vector3(-2010.84, 445.13, 103.02), 288)
    Public NRD1008 As New Location("1008 North Rockford Dr", New Vector3(-2000.783, 366.728, 94.01), LocationType.Residential, New Vector3(-2009.12, 367.43, 94.81), 270)
    Public NRD1006 As New Location("1006 North Rockford Dr", New Vector3(-1993.148, 287.433, 90.97), LocationType.Residential, New Vector3(-1995.33, 300.61, 91.96), 196)
    Public NRD1004 As New Location("1004 North Rockford Dr", New Vector3(-1953.658, 252.294, 84.56), LocationType.Residential, New Vector3(-1970.29, 246.03, 87.81), 289)
    Public NRD1002 As New Location("1002 North Rockford Dr", New Vector3(-1940.945, 205.206, 84.71), LocationType.Residential, New Vector3(-1960.92, 211.98, 86.8), 295)
    Public NRD1001 As New Location("1001 North Rockford Dr", New Vector3(-1878.937, 190.919, 83.594), LocationType.Residential, New Vector3(-1877.27, 215.63, 84.44), 127)
    Public NRD1003 As New Location("1003 North Rockford Dr", New Vector3(-1910.118, 249.629, 85.78), LocationType.Residential, New Vector3(-1887.2, 240.12, 86.45), 211)
    Public NRD1005 As New Location("1005 North Rockford Dr", New Vector3(-1926.447, 292.492, 88.6), LocationType.Residential, New Vector3(-1922.37, 298.18, 89.29), 102)
    Public NRD1007 As New Location("1007 North Rockford Dr", New Vector3(-1944.09, 350.647, 91.82), LocationType.Residential, New Vector3(-1929.54, 369.41, 93.78), 100)
    Public NRD1009 As New Location("1009 North Rockford Dr", New Vector3(-1960.21, 383.541, 93.767), LocationType.Residential, New Vector3(-1942.2, 380.89, 96.12), 27)
    Public NRD1011 As New Location("1011 North Rockford Dr", New Vector3(-1954.286, 448.664, 100.563), LocationType.Residential, New Vector3(-1944.51, 449.53, 102.7), 94)
    Public NRD1015 As New Location("1015 North Rockford Dr", New Vector3(-1941.942, 554.111, 114.35), LocationType.Residential, New Vector3(-1937.57, 551.05, 115.02), 69)
    Public NRD1017 As New Location("1017 North Rockford Dr", New Vector3(-1953.778, 589.897, 118.277), LocationType.Residential, New Vector3(-1928.958, 595.436, 122.28), 65)
    Public NRD1019 As New Location("1019 North Rockford Dr", New Vector3(-1898.356, 619.346, 127.93), LocationType.Residential, New Vector3(-1896.82, 642.375, 130.21), 180)
    Public NRD1022 As New Location("1022 North Rockford Dr", New Vector3(-1859.05, 334.48, 87.88), LocationType.Residential, New Vector3(-1841.58, 313.81, 90.92), 13)
    Public NRD1024 As New Location("1024 North Rockford Dr", New Vector3(-1814.82, 346.16, 87.91), LocationType.Residential, New Vector3(-1808.41, 333.8, 89.37), 33)
    Public NRD1026 As New Location("1026 North Rockford Dr", New Vector3(-1738.51, 388.53, 88.17), LocationType.Residential, New Vector3(-1733.3, 379.93, 89.73), 30)
    Public NRD1028 As New Location("1028 North Rockford Dr", New Vector3(-1674.76, 398.75, 88.28), LocationType.Residential, New Vector3(-1673.37, 386.67, 89.35), 349)
    Public AW1 As New Location("1 Americano Way", New Vector3(-1466.627, 40.889, 53.436), LocationType.Residential, New Vector3(-1467.26, 35.88, 54.54), 351)
    Public AW2 As New Location("2 Americano Way", New Vector3(-1515.466, 30.088, 55.67), LocationType.Residential, New Vector3(-1515.17, 25.24, 56.82), 353)
    Public AW3 As New Location("3 Americano Way", New Vector3(-1568.744, 32.989, 58.65), LocationType.Residential, New Vector3(-1570.5, 23.53, 59.55), 352)
    Public AW4 As New Location("4 Americano Way", New Vector3(-1616.825, 62.221, 60.85), LocationType.Residential, New Vector3(-1629.7, 37.29, 62.94), 335)
    Public SW1 As New Location("1 Steele Way", New Vector3(-910.013, 188.821, 68.969), LocationType.Residential, New Vector3(-903.26, 191.59, 69.45), 120)
    Public SW2 As New Location("1 Steele Way", New Vector3(-954.629, 174.792, 64.76), LocationType.Residential, New Vector3(-949.64, 195.94, 67.39), 166)
    Public PD1 As New Location("1001 Portola Dr", New Vector3(-856.87, 103.36, 52.02), LocationType.Residential, New Vector3(-831.82, 114.7, 55.42), 119)
    Public CaesarsPlace1 As New Location("1 Caesars Pl", New Vector3(-926.348, 16.409, 47.31), LocationType.Residential, New Vector3(-930.26, 19.01, 48.33), 244)
    Public CaesarsPlace2 As New Location("2 Caesars Pl", New Vector3(-891.514, -2.175, 43.04), LocationType.Residential, New Vector3(-895.69, -4.52, 43.8), 304)
    Public CaesarsPlace3 As New Location("3 Caesars Pl", New Vector3(-882.7, 16.31, 43.86), LocationType.Residential, New Vector3(-886.89, 41.79, 48.76), 234)
    Public WMD3673 As New Location("3673 Whispymound Dr", New Vector3(25.86, 560.716, 177.833), LocationType.Residential, New Vector3(45.58, 556.26, 180.08), 16)
    Public WMD3675 As New Location("3675 Whispymound Dr", New Vector3(84.983, 568.64, 181.422), LocationType.Residential, New Vector3(84.89, 562.37, 182.57), 4)
    Public WMD3677 As New Location("3677 Whispymound Dr", New Vector3(119.326, 569.391, 182.554), LocationType.Residential, New Vector3(119.2, 564.51, 183.96), 359)
    Public WMD3679 As New Location("3679 Whispymound Dr", New Vector3(138.758, 570.422, 183.113), LocationType.Residential, New Vector3(163.31, 551.59, 182.34), 187)
    Public WMD3681 As New Location("3681 Whispymound Dr", New Vector3(210.967, 619.599, 186.797), LocationType.Residential, New Vector3(216.06, 620.78, 187.64), 78)
    Public WMD3683 As New Location("3683 Whispymound Dr", New Vector3(217.176, 667.946, 188.55), LocationType.Residential, New Vector3(232, 672.51, 189.95), 39)
    Public NCA2041 As New Location("2041 North Conker Ave", New Vector3(317.672, 569.013, 153.955), LocationType.Residential, New Vector3(317.7, 563.3, 154.45), 4)
    Public NCA2042 As New Location("2042 North Conker Ave", New Vector3(328.543, 497.446, 151.29), LocationType.Residential, New Vector3(316.27, 500.62, 153.18), 226)
    Public NCA2043 As New Location("2043 North Conker Ave", New Vector3(333.456, 474.255, 149.49), LocationType.Residential, New Vector3(331.36, 465.98, 151.19), 7)
    Public NCA2044 As New Location("2044 North Conker Ave", New Vector3(359.103, 441.625, 144.766), LocationType.Residential, New Vector3(346.95, 441.28, 147.7), 306)
    Public NCA2045 As New Location("2045 North Conker Ave", New Vector3(374.333, 436.442, 143.675), LocationType.Residential, New Vector3(372.92, 427.9, 145.68), 26)
    Public DD3550 As New Location("3550 Didion Dr", New Vector3(-470.738, 357.95, 102.73), LocationType.Residential, New Vector3(-468.52, 329.14, 104.15), 319)
    Public DD3552 As New Location("3552 Didion Dr", New Vector3(-445.537, 347.804, 104.17), LocationType.Residential, New Vector3(-444.84, 343.8, 105.36), 11)
    Public DD3554 As New Location("3554 Didion Dr", New Vector3(-400.909, 350.3, 107.781), LocationType.Residential, New Vector3(-408.97, 341.66, 108.91), 312)
    Public DD3556 As New Location("3556 Didion Dr", New Vector3(-368.68, 352.18, 108.927), LocationType.Residential, New Vector3(-359.29, 348.17, 109.39), 62)
    Public DD3558 As New Location("3558 Didion Dr", New Vector3(-350.631, 372.532, 109.507), LocationType.Residential, New Vector3(-328.29, 370.21, 110.02), 34)
    Public DD3560 As New Location("3560 Didion Dr", New Vector3(-307.178, 387.202, 109.705), LocationType.Residential, New Vector3(-297.29, 381.21, 112.05), 32)
    Public DD3562 As New Location("3562 Didion Dr", New Vector3(-261.608, 400.212, 109.444), LocationType.Residential, New Vector3(-239.82, 381.82, 112.43), 83)
    Public DD3564 As New Location("3564 Didion Dr", New Vector3(-202.011, 415.544, 109.273), LocationType.Residential, New Vector3(-214.12, 400.48, 111.11), 16)
    Public DD3566 As New Location("3566 Didion Dr", New Vector3(-184.768, 424.017, 109.846), LocationType.Residential, New Vector3(-178.01, 423.29, 110.88), 101)
    Public DD3567 As New Location("3567 Didion Dr", New Vector3(-91.562, 424.167, 112.621), LocationType.Residential, New Vector3(-72.36, 427.22, 113.04), 106)
    Public DD3569 As New Location("3569 Didion Dr", New Vector3(19.967, 369.569, 111.884), LocationType.Residential, New Vector3(-8.47, 409.27, 120.13), 92)
    Public DD3571 As New Location("3571 Didion Dr", New Vector3(19.967, 369.569, 111.884), LocationType.Residential, New Vector3(41.31, 360.4, 116.04), 238)
    Public DD3651 As New Location("3651 Didion Dr", New Vector3(-318.022, 461.247, 108.009), LocationType.Residential, New Vector3(-312.51, 475.06, 111.82), 129)
    Public DD3581 As New Location("3581 Didion Dr", New Vector3(-347.302, 478.578, 111.87), LocationType.Residential, New Vector3(-355.32, 459.06, 116.47), 6)
    Public DD3589 As New Location("3589 Didion Dr", New Vector3(-480.869, 552.292, 119.27), LocationType.Residential, New Vector3(-500.82, 552.7, 120.43), 297)
    Public DD3587 As New Location("3587 Didion Dr", New Vector3(-468.259, 547.306, 119.666), LocationType.Residential, New Vector3(-458.82, 537.47, 121.46), 352)
    Public DD3585 As New Location("3585 Didion Dr", New Vector3(-437.985, 545.711, 121.246), LocationType.Residential, New Vector3(-437.14, 540.93, 122.13), 352)
    Public DD3583 As New Location("3583 Didion Dr", New Vector3(-379.92, 512.65, 119.83), LocationType.Residential, New Vector3(-386.4, 505.07, 120.41), 330)

    Public MR6085 As New Location("6085 Milton Rd", New Vector3(-656.424, 909.115, 227.743), LocationType.Residential, New Vector3(-659.34, 888.76, 229.25), 13)
    Public MR4589 As New Location("4589 Milton Rd", New Vector3(-597.672, 863.667, 210.149), LocationType.Residential, New Vector3(-599, 852.85, 211.25), 6)
    Public MR4588 As New Location("4588 Milton Rd", New Vector3(-549.421, 836.533, 197.679), LocationType.Residential, New Vector3(-548.63, 827.78, 197.51), 27)
    Public MR4587 As New Location("4587 Milton Rd", New Vector3(-479.269, 800.352, 180.562), LocationType.Residential, New Vector3(-496.07, 799.09, 184.19), 257)
    Public MR4586 As New Location("4586 Milton Rd", New Vector3(-481.381, 742.255, 163.363), LocationType.Residential, New Vector3(-492.34, 737.86, 162.83), 314)
    Public MR4585 As New Location("4585 Milton Rd", New Vector3(-529.918, 700.58, 149.313), LocationType.Residential, New Vector3(-533.23, 708.61, 152.91), 211)
    Public MR2850 As New Location("2850 Milton Rd", New Vector3(-509.213, 625.94, 131.747), LocationType.Residential, New Vector3(-521.85, 627.93, 137.97), 275)
    Public MR2848 As New Location("2848 Milton Rd", New Vector3(-506.436, 576.881, 119.951), LocationType.Residential, New Vector3(-519.14, 594.95, 120.84), 207)
    Public MR3545 As New Location("3545 Milton Rd", New Vector3(-532.605, 536.938, 110.266), LocationType.Residential, New Vector3(-527.28, 518.16, 112.94), 46)
    Public MR2846 As New Location("2846 Milton Rd", New Vector3(-540.44, 542.03, 109.99), LocationType.Residential, New Vector3(-552.33, 540, 110.33), 229)
    Public MR3543 As New Location("3543 Milton Rd", New Vector3(-545.46, 490.96, 103.51), LocationType.Residential, New Vector3(-538.21, 477.76, 103.18), 22)
    Public MR3548 As New Location("3548 Milton Rd", New Vector3(-522.28, 396.8, 93.29), LocationType.Residential, New Vector3(-502.37, 399.91, 97.41), 62)
    Public MR3842 As New Location("3842 Milton Rd", New Vector3(-483.19, 598.37, 126.51), LocationType.Residential, New Vector3(-475.24, 585.9, 128.68), 44)
    Public AJD2103 As New Location("2103 Ace Jones Dr", New Vector3(-1531.82, 438.83, 107.83), LocationType.Residential, New Vector3(-1540.07, 421.57, 110.01), 5)
    Public AJD2105 As New Location("2105 Ace Jones Dr", New Vector3(-1513.27, 433.71, 109.95), LocationType.Residential, New Vector3(-1495.75, 437.85, 112.5), 68)
    Public AJD2107 As New Location("2107 Ace Jones Dr", New Vector3(-1473.67, 518.31, 117.19), LocationType.Residential, New Vector3(-1454.26, 512.88, 117.63), 102)
    Public NSA1102 As New Location("1102 North Sheldon Ave", New Vector3(-1493.39, 511.81, 116.73), LocationType.Residential, New Vector3(-1499.76, 522.91, 118.27), 209)
    Public NSA1107 As New Location("1107 North Sheldon Ave", New Vector3(-1292.56, 631.21, 137.32), LocationType.Residential, New Vector3(-1278.97, 628.8, 142.31), 126)
    Public NSA1109 As New Location("1109 North Sheldon Ave", New Vector3(-1237.13, 655.33, 141.49), LocationType.Residential, New Vector3(-1247.92, 643.67, 142.62), 305)
    Public NSA1111 As New Location("1111 North Sheldon Ave", New Vector3(-1225.09, 665.88, 142.96), LocationType.Residential, New Vector3(-1218.73, 666.08, 144.53), 85)
    Public NSA1113 As New Location("1113 North Sheldon Ave", New Vector3(-1202.93, 691.86, 146.28), LocationType.Residential, New Vector3(-1197.29, 693.49, 147.42), 88)
    Public NSA1115 As New Location("1115 North Sheldon Ave", New Vector3(-1163.07, 747.48, 153.66), LocationType.Residential, New Vector3(-1165.25, 728.7, 155.61), 53)
    Public NSA1117 As New Location("1117 North Sheldon Ave", New Vector3(-1118.39, 775.59, 161.44), LocationType.Residential, New Vector3(-1118.37, 762.3, 164.29), 35)
    Public NSA1112 As New Location("1112 North Sheldon Ave", New Vector3(-1040.12, 792.68, 166.92), LocationType.Residential, New Vector3(-1051.57, 794.85, 167.01), 207)
    Public NSA1110 As New Location("1110 North Sheldon Ave", New Vector3(-1095.98, 786.03, 163.44), LocationType.Residential, New Vector3(-1100.32, 796.2, 166.99), 200)
    Public NSA1108 As New Location("1108 North Sheldon Ave", New Vector3(-1118.1, 781.31, 166.62), LocationType.Residential, New Vector3(-1129.95, 783.95, 163.89), 261)
    Public NSA11152 As New Location("1115 North Sheldon Ave", New Vector3(-995, 789.6, 171.8), LocationType.Residential, New Vector3(-997.1, 768.5, 171.5), 22)
    Public NSA11172 As New Location("1117 North Sheldon Ave", New Vector3(-970.3, 766.9, 174.8), LocationType.Residential, New Vector3(-972.7, 753, 176.4), 344)
    Public NSA1119 As New Location("1119 North Sheldon Ave", New Vector3(-906.4, 789, 185.1), LocationType.Residential, New Vector3(-911.8, 778.4, 187), 9)
    Public NSA1206 As New Location("1206 North Sheldon Ave", New Vector3(-870.1, 797, 190.4), LocationType.Residential, New Vector3(-867.5, 785.2, 191.9), 15)
    Public NSA1121 As New Location("1121 North Sheldon Ave", New Vector3(-827.6, 815.4, 198.5), LocationType.Residential, New Vector3(-824.6, 807.6, 202.6), 23)
    Public NSA1118 As New Location("1118 North Sheldon Ave", New Vector3(-939.7, 792.7, 180.8), LocationType.Residential, New Vector3(-931.1, 807.4, 184.8), 180)
    Public NSA1116 As New Location("1116 North Sheldon Ave", New Vector3(-957.8, 796.5, 177.7), LocationType.Residential, New Vector3(-962.7, 813.1, 177.6), 185)
    Public NSA1114 As New Location("1114 North Sheldon Ave", New Vector3(-993.2, 804, 172.1), LocationType.Residential, New Vector3(-998.7, 816.1, 173), 230)
    Public NDY1201 As New Location("1201 Normandy Dr", New Vector3(-743.5, 817, 213.1), LocationType.Residential, New Vector3(-746.8, 808.1, 215), 340)
    Public NDY1203 As New Location("1203 Normandy Dr", New Vector3(-661.1, 813.9, 199.3), LocationType.Residential, New Vector3(-655.3, 803.4, 199), 6)
    Public NDY1205 As New Location("1205 Normandy Dr", New Vector3(-588.1, 787.4, 188.2), LocationType.Residential, New Vector3(-594.9, 780.8, 189.1), 312)
    Public NDY1207 As New Location("1207 Normandy Dr", New Vector3(-698, 711.6, 157.3), LocationType.Residential, New Vector3(-707.3, 709.9, 162), 286)
    Public NDY2856 As New Location("2856 Normandy Dr", New Vector3(-614.7, 683.4, 149.1), LocationType.Residential, New Vector3(-606.3, 673, 151.6), 350)
    Public NDY2117 As New Location("2117 Normandy Dr", New Vector3(-552.8, 669, 144.1), LocationType.Residential, New Vector3(-559, 664.4, 145.5), 317)
    Public NDY4136 As New Location("4136 Normandy Dr", New Vector3(-561.8, 677.6, 145.2), LocationType.Residential, New Vector3(-565, 683.7, 146.2), 198)
    Public NDY1202 As New Location("1202 Normandy Dr", New Vector3(-666.8, 760.4, 174.2), LocationType.Residential, New Vector3(-663.2, 741.4, 174.3), 341)
    Public NDY1200 As New Location("1200 Normandy Dr", New Vector3(-582.9, 740.8, 183.2), LocationType.Residential, New Vector3(-579.7, 733.8, 184.2), 26)
    Public NDY1198 As New Location("1198 Normandy Dr", New Vector3(-601.2, 801.8, 190.6), LocationType.Residential, New Vector3(-599.8, 806.7, 191.1), 180)


    Public HA1105 As New Location("1105 Hangman Ave", New Vector3(-1358.67, 611.61, 133.36), LocationType.Residential, New Vector3(-1366.6, 611.25, 133.92), 272)
    Public HA2106 As New Location("2106 Hangman Ave", New Vector3(-1354.85, 608.47, 133.3), LocationType.Residential, New Vector3(-1338.54, 605.89, 134.38), 89)
    Public HA1103 As New Location("1103 Hangman Ave", New Vector3(-1353.4, 576.59, 130.56), LocationType.Residential, New Vector3(-1365.62, 567.2, 134.97), 288)
    Public HA2108 As New Location("2108 Hangman Ave", New Vector3(-1363.52, 556.62, 127.66), LocationType.Residential, New Vector3(-1346.59, 560.57, 130.53), 51)
    Public HA1101 As New Location("1101 Hangman Ave", New Vector3(-1412.23, 556.19, 123.1), LocationType.Residential, New Vector3(-1404.22, 561.31, 125.41), 162)
    Public HCA2888 As New Location("2888 Hillcrest Ave", New Vector3(-1047.57, 769.86, 166.75), LocationType.Residential, New Vector3(-1055.87, 761.43, 167.32), 335)
    Public HCA2886 As New Location("2886 Hillcrest Ave", New Vector3(-1043.81, 743.97, 166.23), LocationType.Residential, New Vector3(-1066.26, 727.89, 165.74), 330)
    Public HCA2884 As New Location("2884 Hillcrest Ave", New Vector3(-1017.98, 703.58, 161.5), LocationType.Residential, New Vector3(-1034.18, 686.01, 161.3), 79)
    Public HCA2882 As New Location("2882 Hillcrest Ave", New Vector3(-988.74, 694.61, 157.26), LocationType.Residential, New Vector3(-972.4, 685.69, 158.03), 22)
    Public HCA2880 As New Location("2880 Hillcrest Ave", New Vector3(-932.88, 697.59, 151.78), LocationType.Residential, New Vector3(-931.2, 691.19, 153.47), 19)
    Public HCA2878 As New Location("2878 Hillcrest Ave", New Vector3(-911.61, 699.12, 150.62), LocationType.Residential, New Vector3(-908.08, 694.55, 151.43), 24)
    Public HCA2876 As New Location("2876 Hillcrest Ave", New Vector3(-887.29, 704.76, 149.34), LocationType.Residential, New Vector3(-885.71, 699.43, 151.27), 44)
    Public HCA2874 As New Location("2874 Hillcrest Ave", New Vector3(-860.57, 704.02, 148.31), LocationType.Residential, New Vector3(-853.64, 695.87, 148.78), 42)
    Public HCA2872 As New Location("2872 Hillcrest Ave", New Vector3(-810.79, 712.12, 146.17), LocationType.Residential, New Vector3(-819.78, 697.54, 148.11), 304)
    Public HCA2870 As New Location("2870 Hillcrest Ave", New Vector3(-756.23, 659.58, 142.4), LocationType.Residential, New Vector3(-765.34, 650.81, 145.5), 315)
    Public HCA2868 As New Location("2868 Hillcrest Ave", New Vector3(-750.34, 627.82, 141.7), LocationType.Residential, New Vector3(-752.46, 620.55, 142.41), 333)
    Public HCA2866 As New Location("2866 Hillcrest Ave", New Vector3(-740.26, 603.83, 141.28), LocationType.Residential, New Vector3(-732.92, 593.57, 142.48), 356)
    Public HCA2864 As New Location("2864 Hillcrest Ave", New Vector3(-705.05, 593.98, 141.52), LocationType.Residential, New Vector3(-704.01, 589.44, 141.93), 23)
    Public HCA2862 As New Location("2862 Hillcrest Ave", New Vector3(-691, 600.73, 142.5), LocationType.Residential, New Vector3(-686.51, 596.42, 143.64), 47)
    Public HCA2860 As New Location("2860 Hillcrest Ave", New Vector3(-677.32, 647.44, 148.05), LocationType.Residential, New Vector3(-669.69, 638.66, 149.53), 45)
    Public HCA2858 As New Location("2858 Hillcrest Ave", New Vector3(-677.08, 673.88, 151.18), LocationType.Residential, New Vector3(-661.66, 681.05, 153.92), 164)
    Public HCA2859 As New Location("2859 Hillcrest Ave", New Vector3(-682.87, 669.84, 150.7), LocationType.Residential, New Vector3(-700.48, 648.63, 155.18), 333)



    Public Alta601 As New Location("601 Alta St", New Vector3(148.56, 63.6, 78.25), LocationType.Residential, New Vector3(124.5, 64.8, 79.74), 249)
    Public Alta602 As New Location("602 Alta St", New Vector3(138.26, 38.42, 71.89), LocationType.Residential, New Vector3(112.25, 56.62, 73.51), 257)
    Public Alta1144 As New Location("1144 Alta St", New Vector3(98.88, -85.93, 61.43), LocationType.Residential, New Vector3(64.09, -81.33, 66.7), 342)
    Public Alta1145 As New Location("1145 Alta St", New Vector3(93.45, -101.94, 58.46), LocationType.Residential, New Vector3(74.94, -107.3, 58.19), 313)
    Public VistaDelMarApts As New Location("Vista Del Mar Apartments", New Vector3(-1037.748, -1530.254, 4.529), LocationType.Residential, New Vector3(-1029.53, -1505.1, 4.9), 211)
    Public SRD122 As New Location("122 South Rockford Dr", New Vector3(-799.3, -991.6, 12.86), LocationType.Residential, New Vector3(-813.13, -981.2, 14.14), 157)
    Public VB2057 As New Location("2057 Vespucci Blvd", New Vector3(-666.35, -846.62, 32.5), LocationType.Residential, New Vector3(-662.52, -854.18, 24.46), 9)
    Public BDP1115 As New Location("1115 Boulevard Del Perro", New Vector3(-1609.42, -411.52, 40.67), LocationType.Residential, New Vector3(-1598.22, -421.69, 41.41), 51)
    Public EclipseTowers As New Location("Eclipse Towers", New Vector3(-774.24, 293.42, 85.15), LocationType.Residential, New Vector3(-773.88, 311.63, 85.7), 191)
    Public IntegrityTower As New Location("Integrity Tower", New Vector3(250.66, -641.62, 39.23), LocationType.Residential, New Vector3(267.18, -642.04, 42.02), 83)
    Public Alta3 As New Location("3 Alta St", New Vector3(-236.11, -988.83, 28.45), LocationType.Residential, New Vector3(-261.18, -973.53, 31.22), 215)
    Public SpanAv1150 As New Location("1150 Spanish Ave", New Vector3(254.99, -81.19, 69.45), LocationType.Residential, New Vector3(235.62, -108.02, 74.35), 7)
    Public SpanAv1161 As New Location("1161 Spanish Ave", New Vector3(323.2, -111.72, 67.83), LocationType.Residential, New Vector3(314.31, -128.14, 69.98), 324)
    Public SpanAv1160 As New Location("1160 Spanish Ave", New Vector3(356.82, -124.23, 65.71), LocationType.Residential, New Vector3(352.91, -141.05, 66.69), 334)
    Public TheRoyale As New Location("The Royale", New Vector3(-202.64, 114.13, 69.09), LocationType.Residential, New Vector3(-197.46, 86.8, 69.75), 4)
    Public EclipseLodgeApts As New Location("Eclipse Lodge Apartments", New Vector3(-269.15, 26.8, 54.31), LocationType.Residential, New Vector3(-273.24, 28.41, 54.75), 233)
    Public VespCan1 As New Location("Vespucci Canals", New Vector3(-1094, -959.4, 1.9), LocationType.Residential, New Vector3(-1061.2, -944.7, 2.2), 200)
    Public VespCan2 As New Location("Vespucci Canals", New Vector3(-1058.2, -1040.2, 1.6), LocationType.Residential, New Vector3(-1066, -1051.2, 6.4), 306)
    Public Goma1 As New Location("Apartment, Goma St, Vespucci", New Vector3(-1134.3, -1478, 4), LocationType.Residential, New Vector3(-1145.7, -1465.9, 7.7), 299)
    Public Elgin1 As New Location("Elgin House", New Vector3(-7.7, 164.7, 94.9), LocationType.Residential, New Vector3(-36.1, 170.8, 95), 281)
    Public Elgin2 As New Location("Elgin House", New Vector3(-74.6, 146.5, 80.9), LocationType.Residential, New Vector3(-70.6, 141.6, 81.9), 37)


    Public Barbareno1 As New Location("1 Barbareno Rd, Chumash", New Vector3(-3172.41, 1289.02, 13.41), LocationType.Residential, New Vector3(-3190.34, 1297.37, 19.07), 247)
    Public Barbareno2 As New Location("2 Barbareno Rd, Chumash", New Vector3(-3176.96, 1271.39, 11.98), LocationType.Residential, New Vector3(-3186.28, 1273.3, 12.93), 250)
    Public Barbareno3 As New Location("3 Barbareno Rd, Chumash", New Vector3(-3184.02, 1223.59, 9.64), LocationType.Residential, New Vector3(-3194.51, 1230.73, 10.05), 292)
    Public Barbareno4 As New Location("4 Barbareno Rd, Chumash", New Vector3(-3186.07, 1200.83, 9.16), LocationType.Residential, New Vector3(-3205.32, 1198.95, 9.54), 99)
    Public Barbareno5 As New Location("5 Barbareno Rd, Chumash", New Vector3(-3188.7, 1176.57, 9.05), LocationType.Residential, New Vector3(-3205.7, 1186.27, 9.66), 352)
    Public Barbareno6 As New Location("6 Barbareno Rd, Chumash", New Vector3(-3193.35, 1156.28, 9.18), LocationType.Residential, New Vector3(-3198.94, 1164.24, 9.65), 231)
    Public Barbareno7 As New Location("7 Barbareno Rd, Chumash", New Vector3(-3193.35, 1156.28, 9.18), LocationType.Residential, New Vector3(-3204.1, 1151.96, 9.65), 293)
    Public Barbareno8 As New Location("8 Barbareno Rd, Chumash", New Vector3(-3201.44, 1136.83, 9.46), LocationType.Residential, New Vector3(-3209.43, 1146.02, 9.9), 252)
    Public Barbareno9 As New Location("9 Barbareno Rd, Chumash", New Vector3(-3215.84, 1104.29, 10.02), LocationType.Residential, New Vector3(-3224.81, 1113.62, 10.58), 245)
    Public Barbareno10 As New Location("10 Barbareno Rd, Chumash", New Vector3(-3223.05, 1086.69, 10.33), LocationType.Residential, New Vector3(-3231.81, 1079.29, 10.84), 270)
    Public Barbareno11 As New Location("11 Barbareno Rd, Chumash", New Vector3(-3227.93, 1065.93, 10.71), LocationType.Residential, New Vector3(-3232.38, 1067.4, 11.02), 251)
    Public Barbareno12 As New Location("12 Barbareno Rd, Chumash", New Vector3(-3232.1, 1036.52, 11.25), LocationType.Residential, New Vector3(-3253.59, 1042.83, 11.76), 264)
    Public Barbareno14 As New Location("14 Barbareno Rd, Chumash", New Vector3(-3230.46, 951.99, 12.65), LocationType.Residential, New Vector3(-3237.3, 952.97, 13.14), 275)
    Public Barbareno15 As New Location("15 Barbareno Rd, Chumash", New Vector3(-3226.83, 938.8, 12.94), LocationType.Residential, New Vector3(-3232.34, 934.62, 13.8), 297)
    Public Barbareno16 As New Location("16 Barbareno Rd, Chumash", New Vector3(-3221.6, 928.33, 13.18), LocationType.Residential, New Vector3(-3228.25, 927.8, 13.97), 293)
    Public Barbareno17 As New Location("17 Barbareno Rd, Chumash", New Vector3(-3211.66, 916.67, 13.46), LocationType.Residential, New Vector3(-3218.1, 912.81, 13.99), 313)

    'Public x As New Location("xxx", New Vector3(), LocationType.x, New Vector3(), 0)

    Public Sub initPlaceLists()
        For Each l As Location In ListOfPlaces
            Select Case l.Type
                Case LocationType.AirportArrive
                    lAirportA.Add(l)
                Case LocationType.AirportDepart
                    lAirportD.Add(l)
                Case LocationType.HotelLS
                    lHotelLS.Add(l)
                Case LocationType.Residential
                    lResidential.Add(l)
                Case LocationType.Entertainment
                    lEntertainment.Add(l)
                Case LocationType.Bar
                    lBar.Add(l)
                Case LocationType.FastFood
                    lFastFood.Add(l)
                Case LocationType.Restaurant
                    lRestaurant.Add(l)
                Case LocationType.MotelLS
                    lMotelLS.Add(l)
                Case LocationType.Religious
                    lReligious.Add(l)
                Case LocationType.Shopping
                    lShopping.Add(l)
                Case LocationType.Sport
                    lSport.Add(l)
                Case LocationType.Office
                    lOffice.Add(l)
                Case LocationType.Theater
                    lTheater.Add(l)
                Case LocationType.School
                    lSchool.Add(l)
            End Select
        Next
    End Sub
End Module




'TO-DO LIST

'SHOW MISSION MARKER ARROW AT ORIGIN/DESTINATION
'PED WALKS TO/FROM CAR INSTEAD OF RUNNING
'IMPLEMENT TIP PAY
'   based on speed, driving style & vehicle condition
'EVALUATE:
'   DRIVING STYLE
'   VEHICLE CONDITION (DAMAGE/DIRT)
'   SPEED OF PICK-UP
'MAKE PEDS AROUND ORIGIN AND DESTINATION NOT AGGRESSIVE
'CALM DOWN PEDS SO THEY DONT THINK THEYRE BEING CARJACKED
'   SET RELATIONSHIPS BETWEEN THEM SOMEHOW?
'   OR GROUP THEM?
'CHECK IF PEDS ARE DEAD
'CHECK IF PLAYER IS DEAD
'CHECK IF PLAYER IS WANTED
'DISABLE OTHER MISSION MARKERS / TRIGGERS WHEN THIS MINIGAME IS ACTIVE

