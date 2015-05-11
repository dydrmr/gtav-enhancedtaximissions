Imports GTA
Imports GTA.Math
Imports System
Imports System.Windows.Forms
Public Class SpawnLimousine
    Inherits Script
    Public RND As New Random

    Private Sub SpawnLimo(ByVal sender As Object, ByVal k As KeyEventArgs) Handles MyBase.KeyUp
        If k.KeyCode = Keys.Add Then
            Dim r As Integer = RND.Next(0, 5)
            Dim mdl As GTA.Model

            Select Case r
                Case 0
                    mdl = New Model("superd")
                Case 1
                    mdl = New Model("stretch")
                Case 2
                    mdl = New Model("schafter2")
                Case 3
                    mdl = New Model("washington")
                Case 4
                    mdl = New Model("oracle2")
                Case Else
                    mdl = New Model("schafter2")
            End Select

            Dim Car As Vehicle = World.CreateVehicle(mdl, Game.Player.Character.Position + Game.Player.Character.ForwardVector * 8)
            Car.SetOnGround()
            Car.DirtLevel = 0
            Car.NumberPlate = "L " & RND.Next(0, 10) & RND.Next(0, 10) & RND.Next(0, 10) & RND.Next(0, 10) & RND.Next(0, 10) & RND.Next(0, 10)
            GTA.Native.Function.Call(GTA.Native.Hash.SET_VEHICLE_NUMBER_PLATE_TEXT_INDEX, Car, 1)
            Car.PrimaryColor = VehicleColor.MetallicBlackSteel
            Car.SecondaryColor = VehicleColor.MetallicBlackSteel
            Car.SetMod(VehicleMod.Transmission, 3, False)
            Car.SetMod(VehicleMod.Engine, 3, False)
            Car.SetMod(VehicleMod.Brakes, 3, False)
            Car.MarkAsNoLongerNeeded()
        End If
    End Sub

    Private Sub Repair_Dirt_0(ByVal sender As Object, ByVal k As KeyEventArgs) Handles MyBase.KeyUp
        If k.KeyCode = Keys.Subtract Then
            If Game.Player.Character.IsInVehicle Then
                Game.Player.Character.CurrentVehicle.Repair()
                Game.Player.Character.CurrentVehicle.DirtLevel = 0
            End If
        End If
    End Sub
End Class
