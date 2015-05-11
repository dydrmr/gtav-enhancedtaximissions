Imports GTA
Imports GTA.Math
Imports System
Imports System.Drawing
Imports System.Windows.Forms
Public Class ShowCoordsHeading
    Inherits Script
    Public POS As New Vector3(0, 0, 0)
    Public HDG As Single

    Const UIx As Integer = 80
    Const UIy As Integer = 100

    Private UI As New UIContainer(New Point(1280 - UIx, 720 - UIy), New Size(UIx, UIy), Color.FromArgb(150, 0, 0, 0))

    Public Sub Update(ByVal sender As Object, ByVal e As EventArgs) Handles MyBase.Tick
        POS = Game.Player.Character.Position
        HDG = Game.Player.Character.Heading


        UI.Items.Clear()
        UI.Items.Add(New UIText("X " & Math.Round(POS.X, 2), New Point(0, 0), 0.4F, Color.White, 0, False))
        UI.Items.Add(New UIText("Y " & Math.Round(POS.Y, 2), New Point(0, 20), 0.4F, Color.White, 0, False))
        UI.Items.Add(New UIText("Z " & Math.Round(POS.Z, 2), New Point(0, 40), 0.4F, Color.White, 0, False))
        UI.Items.Add(New UIText("H " & Math.Round(HDG), New Point(0, 60), 0.4F, Color.White, 0, False))
        If Game.Player.Character.IsInVehicle Then
            UI.Items.Add(New UIText("V " & Math.Round(Game.Player.Character.CurrentVehicle.Speed, 2), New Point(0, 80), 0.4F, Color.White, 0, False))
        End If
        UI.Draw()
    End Sub

End Class
