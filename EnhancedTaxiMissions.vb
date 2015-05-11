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

    Public isMinigameActive As Boolean = False
    Public MiniGameStage As MiniGameStages = MiniGameStages.Standby

    Public isSpecialMission As Boolean = False

    Public Origin, Destination As Location
    Public PotentialOrigins, PotentialDestinations As New List(Of Location)

    Public OriginBlip, DestinationBlip As Blip
    Public OriginMarker, DestinationMarker As Integer

    Public Customer As Person
    Public CustomerPed As Ped

    Public isThereASecondCustomer As Boolean = False
    Public isThereAThirdCustomer As Boolean = False
    Public Customer2Ped As Ped
    Public Customer3Ped As Ped

    Public isCustomerPedSpawned As Boolean = False
    Public isDestinationCleared As Boolean = False
    Public isCustomerNudged1 As Boolean = False
    Public isCustomerNudged2 As Boolean = False

    Public NearestLocationDistance As Integer

    Const FareBase As Integer = 6 '3
    Const FarePerMile As Single = 25 '0.3
    Public FareDistance As Single = 0
    Public FareTotal As Integer = 0

    Public IngameMinute As Integer = 0
    Public IngameHour As Integer = 0

    Public UI As New UIContainer(New Point(40, 50), New Size(190, 80), Color.FromArgb(0, 0, 0, 0))
    Public UI_DispatchStatus As String = "DISPATCH-TEXT-INIT"
    Public UI_Origin As String = "ORIG-INIT"
    Public UI_Destination As String = "DEST-INIT"
    Public UI_Dist1 As String = "999"
    Public UI_Dist2 As String = "999"

    Public UIcolor_Header As Color = Color.FromArgb(140, 60, 140, 230)
    Public UIcolor_Status As Color = Color.FromArgb(140, 110, 190, 240)
    Public UIcolor_BG As Color = Color.FromArgb(140, 0, 0, 0)
    Public UItext_White As Color = Color.White
    Public UItext_Dark As Color = Color.FromArgb(180, 80, 80, 80)

    Public updateDist1 As Boolean = False
    Public updateDist2 As Boolean = False

    Public testPed As Ped

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
    End Enum

    Public Sub PRINT(msg As String)
        GTA.UI.Notify(GTA.World.CurrentDayTime.Hours.ToString("D2") & ":" & GTA.World.CurrentDayTime.Minutes.ToString("D2") & ": " & msg)
    End Sub

    Public Sub SavePosition(ByVal sender As Object, ByVal k As KeyEventArgs) Handles MyBase.KeyUp

        'Temporary subroutine that aims to save the players current XYZ coords and heading to an ini file, to speed up the process of entering coordinates for new locations.
        'Haven't quite figured out how to save to an ini file yet.

        If k.KeyCode = Keys.Multiply Then
            'Dim pos As Vector3 = Game.Player.Character.Position
            'Dim hdg As Single = Game.Player.Character.Heading

            'Dim n As String = Game.GetUserInput(64)

            'Settings.SetValue("POSITIONS", n, "(" & Math.Round(pos.X, 2) & ", " & Math.Round(pos.Y, 2) & ", " & Math.Round(pos.Z, 2) & "), " & Math.Round(hdg))
        End If
    End Sub



    Public Sub New()
        'GTA.Native.Function.Call(Native.Hash.SET_PED_POPULATION_BUDGET, 700)
        'GTA.Native.Function.Call(Native.Hash.SET_VEHICLE_POPULATION_BUDGET, 700)

        ListOfPeople.Remove(NonCeleb)

        initPlaceLists()
    End Sub

    Public Sub Update(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Tick

        checkIfMinigameIsActive()

        updateGameTime()
        updateDistances()
        updateRoutes()

        checkIfCloseEnoughToSpawnPed()
        checkIfPlayerHasArrivedAtOrigin()
        checkIfPlayerHasStoppedAtOrigin()
        checkIfPassengerNeedsToBeNudged()
        checkIfPedHasReachedCar()
        checkIfPedHasEnteredCar()
        checkIfCloseEnoughToClearDestination()
        checkIfPlayerHasArrivedAtDestination()
        checkIfPlayerHasStoppedAtDestination()

        refreshUI()

    End Sub




    Public Sub refreshUI()
        UI.Items.Clear()


        UI.Items.Add(New UIRectangle(New Point(0, 0), New Size(190, 25), UIcolor_Header))
        UI.Items.Add(New UIText("Limousine Driver" & " (" & MiniGameStage & ")    " & IngameHour.ToString("D2") & ":" & IngameMinute.ToString("D2"), New Point(3, 1), 0.5, UItext_White, 1, False))


        UI.Items.Add(New UIRectangle(New Point(0, 27), New Size(190, 20), UIcolor_Status))
        UI.Items.Add(New UIText(UI_DispatchStatus, New Point(3, 28), 0.35F, UItext_White, 4, False))



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
    End Sub


    Public Sub checkIfMinigameIsActive()
        If isMinigameActive = True Then
            UI.Enabled = True
        Else
            UI.Enabled = False
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
                    UI_Dist1 = "  (" & dist & " m)"
                Else
                    UI_Dist1 = "  (" & Math.Round(dist / 1000, 2) & " km)"
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

                If dist < 400 Then
                    UI_Dist2 = "  (" & dist & " m)"
                Else
                    UI_Dist2 = "  (" & Math.Round(dist / 1000, 2) & " km)"
                End If
            Else
                UI_Dist2 = ""
            End If
        End If
    End Sub

    Public Sub updateRoutes()
        If IngameMinute Mod 3 = 0 Then
            If MiniGameStage = MiniGameStages.DrivingToOrigin Then
                GTA.Native.Function.Call(Native.Hash.SET_BLIP_ROUTE, OriginBlip.Handle, 0)
                GTA.Native.Function.Call(Native.Hash.SET_BLIP_ROUTE, OriginBlip.Handle, 1)
            End If

            If MiniGameStage = MiniGameStages.DrivingToDestination Then
                GTA.Native.Function.Call(Native.Hash.SET_BLIP_ROUTE, DestinationBlip.Handle, 0)
                GTA.Native.Function.Call(Native.Hash.SET_BLIP_ROUTE, DestinationBlip.Handle, 1)
            End If
        End If
    End Sub

    Public Sub updateGameTime()
        IngameHour = World.CurrentDayTime.Hours
        IngameMinute = World.CurrentDayTime.Minutes
    End Sub

    Public Sub checkIfCloseEnoughToSpawnPed()
        If MiniGameStage = MiniGameStages.DrivingToOrigin Then
            Dim ppos As Vector3 = Game.Player.Character.Position
            Dim opos As Vector3 = Origin.Coords
            Dim distance As Single = World.GetDistance(ppos, opos) 'GTA.Native.Function.Call(Of Single)(Native.Hash.GET_DISTANCE_BETWEEN_COORDS, ppos.X, ppos.Y, ppos.Z, opos.X, opos.Y, opos.Z, 1)

            If distance < 100 Then
                Dim pos As Vector3 = Origin.PedStart
                If isCustomerPedSpawned = False Then
                    isCustomerPedSpawned = True

                    GTA.Native.Function.Call(Native.Hash.CLEAR_AREA_OF_PEDS, pos.X, pos.Y, pos.Z, 15)

                    If Customer.isCeleb = True Then
                        CustomerPed = World.CreatePed(New GTA.Model(Customer.Model), Origin.PedStart, Origin.PedStartHDG)
                    Else
                        CustomerPed = GTA.Native.Function.Call(Of Ped)(Native.Hash.CREATE_RANDOM_PED, pos.X, pos.Y, pos.Z + 0.3)
                        CustomerPed.Heading = Origin.PedStartHDG
                    End If

                    'TO-DO
                    'PUT PEDS INTO A GROUP OR SET THEIR RELATIONSHIPS TO FRIENDLY SO THEY DON'T PANIC WHEN THEY ALL GET INTO THE CAR
                    'GTA.Native.Function.Call(Native.Hash.CREATE_GROUP, 1)
                    'GTA.Native.Function.Call(Native.Hash.SET_PED_AS_GROUP_LEADER, CustomerPed, 1)

                    If isThereASecondCustomer = True Then
                        Customer2Ped = GTA.Native.Function.Call(Of Ped)(Native.Hash.CREATE_RANDOM_PED, pos.X + 0.2, pos.Y + 0.2, pos.Z + 0.3)
                        GTA.Native.Function.Call(Native.Hash.SET_PED_AS_GROUP_MEMBER, Customer2Ped, 1)
                    End If

                    If isThereAThirdCustomer = True Then
                        Customer3Ped = GTA.Native.Function.Call(Of Ped)(Native.Hash.CREATE_RANDOM_PED, pos.X - 0.2, pos.Y - 0.2, pos.Z + 0.3)
                        GTA.Native.Function.Call(Native.Hash.SET_PED_AS_GROUP_MEMBER, Customer3Ped, 1)
                    End If

                    'TO-DO
                    'CREATE MISSION MARKER (LIKE THE ACTUAL TAXI MISSIONS)
                    'GTA.Native.Function.Call(Native.Hash.CREATE_OBJECT, New GTA.Model("mk_arrow").Hash, opos.X, opos.Y, opos.Z, OriginMarker, 1)
                    'PRINT("Ped spawned & area cleared")
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
                'PRINT("Arrived at Origin")
                PlayerHasArrivedAtOrigin()
            End If
        End If
    End Sub

    Public Sub checkIfPlayerHasStoppedAtOrigin()
        If MiniGameStage = MiniGameStages.StoppingAtOrigin Then
            If Game.Player.Character.IsInVehicle = True Then
                If Game.Player.Character.CurrentVehicle.Speed = 0 Then
                    'PRINT("Stopped at Origin")
                    PlayerHasStoppedAtOrigin()
                End If
            End If
        End If
    End Sub

    Public Sub checkIfPassengerNeedsToBeNudged()
        Dim isHonking As Boolean
        isHonking = GTA.Native.Function.Call(Of Boolean)(Native.Hash.IS_PLAYER_PRESSING_HORN, Game.Player)

        If MiniGameStage = MiniGameStages.PedWalkingToCar Then
            If isHonking = True Then
                If isCustomerNudged1 = False Then
                    If CustomerPed.Exists Then
                        CustomerPed.Position = CustomerPed.Position + CustomerPed.ForwardVector * 2
                        isCustomerNudged1 = True
                    End If
                End If
            End If
        End If

        If MiniGameStage = MiniGameStages.PedGettingInCar Then
            If isHonking = True Then
                If isCustomerNudged2 = False Then
                    If CustomerPed.Exists Then
                        CustomerPed.Task.EnterVehicle(Game.Player.Character.CurrentVehicle, VehicleSeat.RightRear, 1000)
                    End If
                    If isThereASecondCustomer = True Then
                        Customer2Ped.Task.EnterVehicle(Game.Player.Character.CurrentVehicle, VehicleSeat.LeftRear, 1000)
                    End If
                    If isThereAThirdCustomer = True Then
                        Customer3Ped.Task.EnterVehicle(Game.Player.Character.CurrentVehicle, VehicleSeat.Passenger, 1000)
                    End If
                    isCustomerNudged2 = True
                End If
            End If
        End If
    End Sub

    Public Sub checkIfPedHasReachedCar()
        If MiniGameStage = MiniGameStages.PedWalkingToCar Then
            If CustomerPed.Exists = True Then
                Dim tgt As Vector3 = Game.Player.Character.Position
                Dim ppo As Vector3 = CustomerPed.Position
                Dim distance As Single = GTA.Native.Function.Call(Of Single)(Native.Hash.GET_DISTANCE_BETWEEN_COORDS, tgt.X, tgt.Y, tgt.Z, ppo.X, ppo.Y, ppo.Z, 1)

                If distance < 8 Then
                    'PRINT("Ped has reached car")
                    PedHasReachedCar()
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
                        'PRINT("Ped has taken a seat")
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
        System.Diagnostics.Debug.Print("Made it to point 1")
        If MiniGameStage = MiniGameStages.DrivingToDestination Then
            Dim ppos As Vector3 = Game.Player.Character.Position
            Dim dpos As Vector3 = Destination.Coords
            Dim distance As Single = World.GetDistance(ppos, dpos)

            If distance < 60 Then
                Dim pos As Vector3 = Destination.PedStart
                If isDestinationCleared = False Then
                    isDestinationCleared = True

                    'GTA.Native.Function.Call(Native.Hash.CLEAR_AREA_OF_PEDS, pos.X, pos.Y, pos.Z, 15)
                    'PRINT("Destination Cleared")
                End If
            End If

        End If
        System.Diagnostics.Debug.Print("Made it to point 2")
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
                    PlayerHasStoppedAtDestination()
                End If
            End If
        End If
    End Sub









    Public Sub ToggleMinigame(ByVal sender As Object, ByVal k As KeyEventArgs) Handles MyBase.KeyUp

        If Game.Player.IsOnMission = True Then Exit Sub

        If k.KeyCode = Keys.L Then

            If isMinigameActive = True Then
                'PRINT("Limo missions ended.")
                EndMinigame()
                isMinigameActive = False
            Else

                If Game.Player.Character.IsInVehicle Then
                    Dim maxSeats As Integer = GTA.Native.Function.Call(Of Integer)(Native.Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, Game.Player.Character.CurrentVehicle)

                    If maxSeats >= 3 Then
                        Dim veh As String = Game.Player.Character.CurrentVehicle.DisplayName

                        If veh = "TAXI" Or veh = "STRETCH" Or veh = "SCHAFTER" Or veh = "SUPERD" Or veh = "ORACLE" Or veh = "WASHINGT" Then
                            'PRINT("Limo missions started.")
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

        NearestLocationDistance = 100000

        FareTotal = 0

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
                GTA.Native.Function.Call(Native.Hash.SET_BLIP_ROUTE, OriginBlip.Handle, 0)
            End If
        End If

        If DestinationBlip IsNot Nothing Then
            If DestinationBlip.Exists Then
                GTA.Native.Function.Call(Native.Hash.SET_BLIP_ROUTE, DestinationBlip.Handle, 0)
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
            FareDistance = World.GetDistance(StartPoint, EndPoint)
        End If
        FareDistance *= 0.621371
        FareTotal = CInt(Math.Round(FareBase + (FareDistance * FarePerMile)))

        PRINT("DIS: " & Math.Round(FareDistance, 2) & " mi / $" & FareTotal)
    End Sub

    Public Sub TaskSequenceTest(ByVal sender As Object, ByVal k As KeyEventArgs) Handles MyBase.KeyUp

        If k.KeyCode = Keys.Multiply Then
            Dim pos As Vector3 = Game.Player.Character.Position + Game.Player.Character.ForwardVector * 4
            testPed = GTA.Native.Function.Call(Of Ped)(Native.Hash.CREATE_RANDOM_PED, pos.X, pos.Y, pos.Z)

            Dim ts As New TaskSequence()
            ts.AddTask.UseMobilePhone()
            ts.Close()
            testPed.IsEnemy = False
        End If

    End Sub

    Public Sub DeleteTemporaryPed(ByVal sender As Object, ByVal k As KeyEventArgs) Handles MyBase.KeyUp

        If k.KeyCode = Keys.Divide Then
            testPed.MarkAsNoLongerNeeded()
        End If

    End Sub




    Private Sub StartMission()

        Dim r As Integer = RND.Next(0, 10)
        If r < 1 Then
            isSpecialMission = True
            GenerateSpecialMissionLocations()
        Else
            isSpecialMission = False
            GenerateGenericMissionLocations()
        End If

        SelectValidOrigin(PotentialOrigins)
        SelectValidDestination(PotentialDestinations)

        If isSpecialMission = True Then
            SelectSpecialCustomers()
        Else
            SelectGenericCustomers()
        End If

        OriginBlip = World.CreateBlip(Origin.Coords)
        OriginBlip.Color = BlipColor.Blue
        GTA.Native.Function.Call(Native.Hash.SET_BLIP_ROUTE, OriginBlip.Handle, 1)

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
            dist = GTA.Native.Function.Call(Of Single)(Native.Hash.GET_DISTANCE_BETWEEN_COORDS, l.Coords.X, l.Coords.Y, l.Coords.Z, ppos.X, ppos.Y, ppos.Z, 1)
            If dist < NearestLocationDistance Then
                NearestLocation = l
                NearestLocationDistance = dist
            End If
        Next

        If NearestLocationDistance > 700 Then
            NearestLocationDistance += 30%
        Else
            NearestLocationDistance = 700
        End If

        Dim r As Integer
        Dim distance As Single
        Do
            r = RND.Next(0, Places.Count)
            Origin = Places(r)
            Dim ppos As Vector3 = Game.Player.Character.Position
            distance = GTA.Native.Function.Call(Of Single)(Native.Hash.GET_DISTANCE_BETWEEN_COORDS, Origin.Coords.X, Origin.Coords.Y, Origin.Coords.Z, ppos.X, ppos.Y, ppos.Z, 1)
        Loop While distance > NearestLocationDistance Or Origin.isValidDestination = False  'Or distance < 50

        UI_Origin = Origin.Name
    End Sub

    Private Sub SelectValidDestination(Places As List(Of Location))

        If Places.Count = 0 Then Places.AddRange(ListOfPlaces)

        Dim r As Integer
        Dim distance As Single
        Do
            r = RND.Next(0, Places.Count)
            Destination = Places(r)
            distance = GTA.Native.Function.Call(Of Single)(Native.Hash.GET_DISTANCE_BETWEEN_COORDS, Origin.Coords.X, Origin.Coords.Y, Origin.Coords.Z, Destination.Coords.X, Destination.Coords.Y, Destination.Coords.Z, 1)
        Loop While Origin.Name = Destination.Name Or distance < 500

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

        r = RND.Next(0, 2)
        If r = 0 Then
            isThereASecondCustomer = True

            Dim t As Integer = RND.Next(0, 2)
            If t = 0 Then
                isThereAThirdCustomer = True
                GTA.UI.ShowSubtitle("3 PAX", 5000)
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

        MiniGameStage = MiniGameStages.StoppingAtOrigin
    End Sub

    Private Sub PlayerHasStoppedAtOrigin()

        Dim ppos As Vector3
        If Game.Player.Character.IsInVehicle Then
            ppos = Game.Player.Character.CurrentVehicle.Position
        Else
            ppos = Game.Player.Character.Position
        End If


        If CustomerPed.Exists Then
            CustomerPed.Task.GoTo(ppos, False)
        End If

        If isThereASecondCustomer = True Then
            If Customer2Ped.Exists = True Then
                Customer2Ped.Task.GoTo(ppos, False)
            End If
        End If

        If isThereAThirdCustomer = True Then
            If Customer3Ped.Exists = True Then
                Customer3Ped.Task.GoTo(ppos, False)
            End If
        End If

        'TO-DO
        'REMOVE GPS ROUTE TO ORIGIN BLIP
        'OriginBlip.Remove
        'PRINT("Origin Blip Removed")

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

        DestinationBlip = World.CreateBlip(Destination.Coords)
        DestinationBlip.Color = BlipColor.Blue

        GTA.Native.Function.Call(Native.Hash.SET_BLIP_ROUTE, DestinationBlip.Handle, 1)
        'PRINT("DestinationBlip: " & DestinationBlip)

        UI_DispatchStatus = "Please drive the customer to the destination"
        If isThereASecondCustomer = True Then
            UI_DispatchStatus = "Please drive the customers to their destination"
        End If
        MiniGameStage = MiniGameStages.DrivingToDestination
    End Sub

    Private Sub PlayerHasArrivedAtDestination()
        updateDist2 = False
        MiniGameStage = MiniGameStages.StoppingAtDestination
    End Sub

    Private Sub PlayerHasStoppedAtDestination()

        MiniGameStage = MiniGameStages.PedGettingOut

        'TO-DO
        'REMOVE GPS ROUTE TO DESTINATION BLIP
        'DestinationBlip.Remove
        'PRINT("Destination Blip Removed")

        CustomerPed.Task.LeaveVehicle(Game.Player.Character.CurrentVehicle, True)

        If isThereASecondCustomer = True Then
            Customer2Ped.Task.LeaveVehicle(Game.Player.Character.CurrentVehicle, True)
        End If
        If isThereAThirdCustomer = True Then
            Customer3Ped.Task.LeaveVehicle(Game.Player.Character.CurrentVehicle, True)
        End If

        Wait(250)

        Dim isDestinationSet As Boolean
        If Destination.PedEnd.X = 0 And Destination.PedEnd.Y = 0 And Destination.PedEnd.Z = 0 Then
            isDestinationSet = False
        Else
            isDestinationSet = True
        End If

        If isDestinationSet = False Then
            CustomerPed.Task.GoTo(Destination.PedStart, False)
        Else
            CustomerPed.Task.GoTo(Destination.PedEnd, False)
        End If

        CustomerPed.MarkAsNoLongerNeeded()

        If isThereASecondCustomer = True Then
            If isDestinationSet = False Then
                Customer2Ped.Task.GoTo(Destination.PedStart, False)
            Else
                Customer2Ped.Task.GoTo(Destination.PedEnd, False)
            End If
            Customer2Ped.MarkAsNoLongerNeeded()
        End If

        If isThereAThirdCustomer = True Then
            If isDestinationSet = False Then
                Customer3Ped.Task.GoTo(Destination.PedStart, False)
            Else
                Customer3Ped.Task.GoTo(Destination.PedEnd, False)
            End If
            Customer3Ped.MarkAsNoLongerNeeded()
        End If

        payPlayer(FareTotal)

        'TO-DO
        'AUTOSAVE

        StartMinigame()
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

    'SCHOOL
    'Public ULSA1 As New Location("ULSA Campus", New Vector3(-1572.412, 175.073, 57.622), LocationType.School, New Vector3(), 0)
    'Public ULSA2 As New Location("ULSA Campus", New Vector3(-1644.79, 141.821, 61.468), LocationType.School, New Vector3(), 0)

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
    Public CoolBeans As New Location("Cool Beans, Legion Square", New Vector3(260.26, -970.32, 28.7), LocationType.FastFood, New Vector3(263.08, -981.74, 29.36), 86)

    'RESTAURANT
    Public LaSpada As New Location("La Spada", New Vector3(-1046.724, -1398.146, 4.949), LocationType.Restaurant, New Vector3(-1038.01, -1396.84, 5.55), 84)
    Public ChebsEaterie As New Location("Chebs Eaterie", New Vector3(-730.21, -330.45, 35), LocationType.Restaurant, New Vector3(-735.26, -319.63, 36.22), 187)
    Public CafeRedemption As New Location("Cafe Redemption", New Vector3(-641.08, -308.14, 34.21), LocationType.Restaurant, New Vector3(-634.26, -302.17, 35.06), 131)

    'BAR
    Public PipelineInn As New Location("Pipeline Inn", New Vector3(-2182.395, -391.984, 12.83), LocationType.Bar, New Vector3(-2192.54, -389.54, 13.47), 249)
    Public EclipseLounge As New Location("Eclipse Lounge", New Vector3(-83, 246.52, 99.77), LocationType.Bar, New Vector3(-84.96, 235.74, 100.56), 2)
    Public MojitoInn As New Location("Mojito Inn, Paleto Bay", New Vector3(-130.08, 6396.05, 30.88), LocationType.Bar, New Vector3(-121.04, 6394.28, 31.49), 82)
    Public Henhouse As New Location("The Hen House, Paleto Bay", New Vector3(-295.27, 6248.5, 30.82), LocationType.Bar, New Vector3(-295.15, 6259.08, 31.49), 174)
    Public BayviewLodge As New Location("Bayview Lodge, Paleto Bay", New Vector3(-700, 5816.39, 16.68), LocationType.Bar, New Vector3(-697.98, 5802.34, 17.33), 54)

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

    'ENTERTAINMENT
    Public DelPerroPier As New Location("Del Perro Pier", New Vector3(-1624.56, -1008.23, 12.4), LocationType.Entertainment, New Vector3(-1638, -1012.97, 13.12), 346) With {.PedEnd = New Vector3(-1841.98, -1213.19, 13.02)}
    Public Tramway As New Location("Pala Springs Aerial Tramway", New Vector3(-771.53, 5582.98, 33.01), LocationType.Entertainment, New Vector3(-755.66, 5583.63, 36.71), 91) With {.PedEnd = New Vector3(-745.23, 5594.77, 41.65)}
    Public LSGC As New Location("Los Santos Gun Club", New Vector3(16.86, -1125.85, 29.3), LocationType.Entertainment, New Vector3(20.24, -1107.24, 29.8), 173)

    'THEATER
    Public LosSantosTheater As New Location("Los Santos Theater", New Vector3(345.33, -867.2, 28.72), LocationType.Theater, New Vector3(353.7, -874.09, 29.29), 8)
    Public TenCentTheater As New Location("Ten Cent Theater", New Vector3(401.16, -711.92, 28.7), LocationType.Theater, New Vector3(394.68, -710.04, 29.28), 254)

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

    'MOTEL
    Public PerreraBeach As New Location("Perrera Beach Motel", New Vector3(-1480.4, -669.76, 28.23), LocationType.MotelLS, New Vector3(-1478.68, -649.89, 29.58), 162) With {.PedEnd = New Vector3(-1479.64, -674.43, 29.04)}
    Public DreamView As New Location("Dream View Motel, Paleto Bay", New Vector3(-94.06, 6310.33, 31.02), LocationType.MotelBC, New Vector3(-106.33, 6315.21, 31.49), 212)

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
    Public DD3583 As New Location("3583 Didion Dr", New Vector3(-379.833, 572.336, 119.711), LocationType.Residential, New Vector3(-386.4, 505.07, 120.41), 330)

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

    Public Alta601 As New Location("601 Alta St", New Vector3(148.56, 63.6, 78.25), LocationType.Residential, New Vector3(124.5, 64.8, 79.74), 249)
    Public Alta602 As New Location("602 Alta St", New Vector3(138.26, 38.42, 71.89), LocationType.Residential, New Vector3(108.75, 54.78, 77.77), 282)
    Public Alta1144 As New Location("1144 Alta St", New Vector3(98.88, -85.93, 61.43), LocationType.Residential, New Vector3(64.09, -81.33, 66.7), 342)
    Public Alta1145 As New Location("1145 Alta St", New Vector3(93.45, -101.94, 58.46), LocationType.Residential, New Vector3(74.94, -107.3, 58.19), 313)
    Public VistaDelMarApts As New Location("Vista Del Mar Apartments", New Vector3(-1037.748, -1530.254, 4.529), LocationType.Residential, New Vector3(-1029.53, -1505.1, 4.9), 211)

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
            End Select
        Next
    End Sub
End Module




'TO-DO LIST

'DELETE BLIPS UPON REACHING THEM
'PAY PLAYER
'SHOW MISSION MARKER ARROW AT ORIGIN/DESTINATION
'PED WALKS TO/FROM CAR INSTEAD OF RUNNING
'PED PAUSES AT ARRIVAL POINT BEFORE BECOMING NOLONGERNEEDED
'IMPLEMENT TIP PAY
'   based on speed, driving style & vehicle condition
'EVALUATE:
'   DRIVING STYLE
'   VEHICLE CONDITION (DAMAGE/DIRT)
'   SPEED OF PICK-UP AND ARRIVAL
'MAKE PEDS AROUND ORIGIN AND DESTINATION NOT AGGRESSIVE
'CALM DOWN PEDS SO THEY DONT THINK THEYRE BEING CARJACKED
'   SET RELATIONSHIPS BETWEEN THEM SOMEHOW?
'   OR GROUP THEM?
'CHECK IF PEDS ARE DEAD
'DISABLE OTHER MISSION MARKERS / TRIGGERS WHEN THIS MINIGAME IS ACTIVE

