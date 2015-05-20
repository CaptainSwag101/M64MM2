Imports System
Imports System.IO

Public Class MainForm
    Private Declare Function GetKeyPress Lib "user32" Alias "GetAsyncKeyState" (ByVal key As Integer) As Integer

    Private ChangeCamera As Boolean = False
    Private CameraUnfrozen As Boolean = False
    Private Base As Long
    Public Shared EmuOpen As Boolean = False
    Private Key3WasUp As Boolean = True
    Private ctrlkey As Boolean
    Private AnimList As New List(Of Animation)
    Private AnimData As Dictionary(Of String, String) = New Dictionary(Of String, String)
    Private LastCBox1Index As Integer
    Private TestOnce As Boolean = False

    Private ReadOnly Property CB1AnimIndex As Integer
        Get
            For Each anim As Animation In AnimList
                If anim.Value = ComboBox1.SelectedValue Then Return anim.Index
            Next
        End Get
    End Property

    Private ReadOnly Property CB2AnimIndex As Integer
        Get
            For Each anim As Animation In AnimList
                If anim.Value = ComboBox2.SelectedValue Then Return anim.Index
            Next
        End Get
    End Property

    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()

        Try
            Using sr As New StreamReader("animation_data.txt")
                Do While sr.Peek() >= 0
                    Dim rawLine As String
                    rawLine = sr.ReadLine()
                    Dim step1 As String = rawLine.Trim()
                    Dim step2 As String = step1.TrimStart("0")
                    Dim step3 As String = step2.TrimStart("x")
                    Dim splitLine() As String = Split(step3, " = ")
                    AnimData.Add(splitLine(0), splitLine(1))
                    AnimList.Add(New Animation(splitLine(0), splitLine(1), CInt(splitLine(2))))
                Loop
            End Using
        Catch e As Exception
            MessageBox.Show("Error reading animation data file:" & vbCrLf & e.Message)
        End Try

        If AnimData.Count > 0 Then
            ComboBox1.DataSource = New BindingSource(AnimData, Nothing)
            ComboBox1.DisplayMember = "Value"
            ComboBox1.ValueMember = "Key"
            ComboBox2.DataSource = New BindingSource(AnimData, Nothing)
            ComboBox2.DisplayMember = "Value"
            ComboBox2.ValueMember = "Key"
            AddHandler ComboBox1.SelectedValueChanged, AddressOf ComboBox1_SelectedValueChanged
            AddHandler ComboBox2.SelectedValueChanged, AddressOf ComboBox2_SelectedValueChanged
            AddHandler ResetAnimationSwapsMenuItem.Click, AddressOf ResetAnimations
            ComboBox1.SelectedIndex = 0
            ComboBox2.SelectedIndex = 0
            LastCBox1Index = 0
            ComboBox1.Refresh()
            ComboBox2.Refresh()
        End If

        If GetEmuProcess("Project64") = Nothing Then
            EmuOpen = False
        Else
            EmuOpen = True
        End If

        Timer1.Enabled = True
        Timer1.Interval = 1
    End Sub

    Public Function GetChunks(value As String, chunkSize As Integer) As List(Of String)
        Dim bytes As New List(Of String)
        While value.Length > chunkSize
            bytes.Add(value.Substring(0, chunkSize))
            value = value.Substring(chunkSize)
        End While
        If value <> "" Then
            bytes.Add(value)
        End If
        Return bytes
    End Function

    Private Sub WriteAnimationSwap()
        If EmuOpen = True And Base > 0 Then
            WriteInteger("Project64", Base + &H64040 + ((CB1AnimIndex + 1) * 8), Integer.Parse(GetChunks(ComboBox2.SelectedValue, 8)(0), Globalization.NumberStyles.HexNumber))
            WriteInteger("Project64", Base + &H64044 + ((CB1AnimIndex + 1) * 8), Integer.Parse(GetChunks(ComboBox2.SelectedValue, 8)(1), Globalization.NumberStyles.HexNumber))
        End If
    End Sub

    Private Function CurrentAnimInRAM() As String
        Dim Whole As String = ""
        Dim Half1 As String = ""
        Dim Half2 As String = ""
        If EmuOpen = True And Base > 0 Then
            For x = 0 To 3
                Dim nextPart As String = Hex(CStr(ReadByte("Project64", Base + &H64040 + ((CB1AnimIndex + 1) * 8) + x)(0)))
                If nextPart.Count = 1 Then nextPart = "0" & nextPart
                Half1 = Half1 & StrReverse(nextPart)
            Next

            For x = 0 To 3
                Dim nextPart As String = Hex(CStr(ReadByte("Project64", Base + &H64044 + ((CB1AnimIndex + 1) * 8) + x)(0)))
                If nextPart.Count = 1 Then nextPart = "0" & nextPart
                Half2 = Half2 & StrReverse(nextPart)
            Next

            Whole = StrReverse(Half1) & StrReverse(Half2)
            Return Whole
        End If
        Return "Error"
    End Function

    Private Sub ResetAnimations()
        For Each anim As Animation In AnimList
            WriteInteger("Project64", Base + &H64040 + ((anim.Index + 1) * 8), Integer.Parse(GetChunks(anim.Value, 8)(0), Globalization.NumberStyles.HexNumber))
            WriteInteger("Project64", Base + &H64044 + ((anim.Index + 1) * 8), Integer.Parse(GetChunks(anim.Value, 8)(1), Globalization.NumberStyles.HexNumber))
        Next
        ComboBox2.SelectedIndex = ComboBox1.SelectedIndex
    End Sub

    Private Sub GetBase(Optional silent As Boolean = True)
        ' Get the base RAM address of the emulated memory block by searching for the constant value of SM64's first RAM address
        Label1.Text = "Scanning for base address..."
        Label1.Refresh()
        Base = GetBaseAddress("Project64", silent)
        If Base > 0 Then
            If silent = False Then
                MessageBox.Show("The base address is: " & Hex(Base))
            End If
            Label1.Text = "The base address is: " & Hex(Base)
        Else
            Label1.Text = "Base address not found!"
        End If
    End Sub

    Private Sub Freeze()
        ChangeCamera = False
        WriteInteger("Project64", Base + &H33C848, &H80000000)
    End Sub

    Private Sub Unfreeze()
        ChangeCamera = False
        CameraUnfrozen = True
        WriteInteger("Project64", Base + &H33C848, 0)
    End Sub

    Private Sub ChangeCameraType()
        ChangeCamera = Not ChangeCamera
        If ChangeCamera = True Then
            b_ChangeCameraType.Text = "Go to new area"
        Else
            b_ChangeCameraType.Text = "Change Camera Type"
        End If
    End Sub

    Private Sub b_Freeze_Click(sender As Object, e As EventArgs) Handles b_Freeze.Click
        If EmuOpen = True And Base > 0 Then Freeze()
    End Sub

    Private Sub b_Unfreeze_Click(sender As Object, e As EventArgs) Handles b_Unfreeze.Click
        If EmuOpen = True And Base > 0 Then Unfreeze()
    End Sub

    Private Sub b_ChangeCameraType_Click(sender As Object, e As EventArgs) Handles b_ChangeCameraType.Click
        If EmuOpen = True And Base > 0 Then ChangeCameraType()
    End Sub

    Private Sub TimerEventProcessor(myObject As Object, ByVal myEventArgs As EventArgs) Handles Timer1.Tick
        Timer1.Stop() ' Don't let the timer tick again until we're done processing the current tick (this precaution may be unnecessary)

        ' Main program update call
        If GetEmuProcess("Project64") = Nothing Then
            EmuOpen = False
        Else
            EmuOpen = True
        End If

        If EmuOpen = True Then
            If Base > 0 Then
                ' Check if base address is still correct
                If ReadInteger("Project64", Base) <> &H3C1A8032 Then ' If our old base is not valid, we need to start looking for a new one
                    Base = 0
                    Timer1.Enabled = True ' Re-enable the timer so we can start to scan for a new base address
                    Exit Sub
                End If

                If TestOnce = False Then
                    ComboBox2.SelectedValue = CurrentAnimInRAM()
                    TestOnce = True
                End If

                ' Handle key input (for hotkeys, etc.)
                HandleInput()

                ' Sometimes exiting first-person while the camera is frozen will result in a glitched state where Mario is stuck in first-person.
                ' This checks to see if this has happened, and forcibly fixes the camera if needed.
                If ReadLong("Project64", Base + &H33C848) >= &HA2000000L Then WriteInteger("Project64", Base + &H33C848, &H80000000)

                ' If we are changing camera modes, repeatedly force the camera into frozen mode.
                If ChangeCamera = True Then WriteInteger("Project64", Base + &H33C848, &H80000000)
            Else
                GetBase()
            End If
        Else
            Label1.Text = "Project64 isn't open!"
        End If

        Timer1.Enabled = True ' Re-enable the timer so this sub will continue to be called repeatedly
    End Sub

    Private Sub HandleInput()
        If GetKeyPress(Keys.ControlKey) And GetKeyPress(Keys.D1) Then
            Freeze()
        ElseIf GetKeyPress(Keys.ControlKey) And GetKeyPress(Keys.D2) Then
            Unfreeze()
        ElseIf GetKeyPress(Keys.ControlKey) And GetKeyPress(Keys.D3) And Key3WasUp Then
            Key3WasUp = False
            If CameraUnfrozen = True Then
                b_ChangeCameraType.Text = "Change Camera Type"
                CameraUnfrozen = False
                Exit Sub
            End If
            ChangeCameraType()
        End If

        If GetKeyPress(Keys.D3) = False Then
            Key3WasUp = True
        Else
            Key3WasUp = False
        End If
    End Sub

    Private Sub AboutM64MovieMaker20ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutM64MovieMaker20ToolStripMenuItem.Click
        Dim AboutDialog As New AboutForm
        AboutDialog.ShowDialog()
    End Sub

    Private Sub ComboBox1_SelectedValueChanged(ByVal sender As Object, ByVal e As EventArgs)
        If UndoPreviousAnimationSwapsMenuItem.Checked And EmuOpen = True And Base > 0 Then
            For Each anim As Animation In AnimList
                If anim.Value = DirectCast(ComboBox2.Items(LastCBox1Index), KeyValuePair(Of String, String)).Key Then
                    WriteInteger("Project64", Base + &H64040 + ((anim.Index + 1) * 8), Integer.Parse(GetChunks(anim.Value, 8)(0), Globalization.NumberStyles.HexNumber))
                    WriteInteger("Project64", Base + &H64044 + ((anim.Index + 1) * 8), Integer.Parse(GetChunks(anim.Value, 8)(1), Globalization.NumberStyles.HexNumber))
                    Exit For
                End If
            Next
        End If
        LastCBox1Index = ComboBox1.SelectedIndex

        If EmuOpen = True And Base > 0 Then ComboBox2.SelectedValue = CurrentAnimInRAM()
    End Sub

    Private Sub ComboBox2_SelectedValueChanged(sender As Object, e As EventArgs)
        WriteAnimationSwap()
    End Sub

    Private Sub RetainAnimationSwapsMenuItem_Click(sender As Object, e As EventArgs) Handles RetainAnimationSwapsMenuItem.Click
        RetainAnimationSwapsMenuItem.Checked = True
        RetainAnimationSwapsMenuItem.CheckState = CheckState.Checked

        UndoPreviousAnimationSwapsMenuItem.Checked = False
        UndoPreviousAnimationSwapsMenuItem.CheckState = CheckState.Unchecked
    End Sub

    Private Sub UndoPreviousAnimationSwapsMenuItem_Click(sender As Object, e As EventArgs) Handles UndoPreviousAnimationSwapsMenuItem.Click
        UndoPreviousAnimationSwapsMenuItem.Checked = True
        UndoPreviousAnimationSwapsMenuItem.CheckState = CheckState.Checked

        RetainAnimationSwapsMenuItem.Checked = False
        RetainAnimationSwapsMenuItem.CheckState = CheckState.Unchecked
    End Sub
End Class

Public Class Animation
    Public Value As String
    Public Description As String
    Public Index As Integer

    Public Sub New(val As String, desc As String, ind As Integer)
        Value = val
        Description = desc
        Index = ind
    End Sub
End Class
