Imports System.Drawing
Imports System.Windows.Forms

' Handles all DataGridView formatting and projection for DTC data so MainForm only triggers refreshes.
Public Class DtcGridPresenter
    Private ReadOnly _grid As DataGridView
    Private ReadOnly _searchBox As TextBox
    Private ReadOnly _categoryCombo As ComboBox
    Private ReadOnly _countLabel As Label
    Private ReadOnly _allDtc As List(Of DTCInfo)
    Private ReadOnly _filteredDtc As List(Of DTCInfo)

    Public Sub New(grid As DataGridView, searchBox As TextBox, categoryCombo As ComboBox, countLabel As Label, allDtc As List(Of DTCInfo), filteredDtc As List(Of DTCInfo))
        _grid = grid
        _searchBox = searchBox
        _categoryCombo = categoryCombo
        _countLabel = countLabel
        _allDtc = allDtc
        _filteredDtc = filteredDtc
    End Sub

    Public Sub InitializeGrid()
        _grid.Columns.Clear()

        Dim colCode As New DataGridViewTextBoxColumn() With {.Name = "Code", .HeaderText = "Kod", .Width = 80, .ReadOnly = True}
        _grid.Columns.Add(colCode)

        Dim colDesc As New DataGridViewTextBoxColumn() With {.Name = "Description", .HeaderText = "Açıklama", .Width = 380, .ReadOnly = True}
        _grid.Columns.Add(colDesc)

        Dim colCat As New DataGridViewTextBoxColumn() With {.Name = "Category", .HeaderText = "Kategori", .Width = 100, .ReadOnly = True}
        _grid.Columns.Add(colCat)

        Dim colSev As New DataGridViewTextBoxColumn() With {.Name = "Severity", .HeaderText = "Şiddet", .Width = 90, .ReadOnly = True}
        _grid.Columns.Add(colSev)

        Dim colStatus As New DataGridViewTextBoxColumn() With {.Name = "Status", .HeaderText = "Durum", .Width = 70, .ReadOnly = True}
        _grid.Columns.Add(colStatus)

        AddHandler _grid.CellDoubleClick, AddressOf DgvDTCList_CellDoubleClick
        AddHandler _grid.CellFormatting, AddressOf DgvDTCList_CellFormatting
        AddHandler _searchBox.TextChanged, AddressOf TxtDTCSearch_TextChanged
        AddHandler _categoryCombo.SelectedIndexChanged, AddressOf CmbDTCCategory_SelectedIndexChanged
    End Sub

    Public Sub RefreshGrid()
        _grid.Rows.Clear()

        For Each dtc In _filteredDtc
            Dim rowIndex = _grid.Rows.Add()
            Dim row = _grid.Rows(rowIndex)
            row.Cells("Code").Value = dtc.Code
            row.Cells("Description").Value = If(String.IsNullOrEmpty(dtc.DescriptionTR), dtc.DescriptionEN, dtc.DescriptionTR)
            row.Cells("Category").Value = dtc.Category
            row.Cells("Severity").Value = GetSeverityDisplayText(dtc.Severity)
            row.Cells("Status").Value = GetStatusDisplayText(dtc)
            row.Tag = dtc
        Next

        _countLabel.Text = $"{_filteredDtc.Count} arıza kodu"
    End Sub

    Public Sub ApplyFilter()
        Dim searchText = If(_searchBox.Text, "").ToLower().Trim()
        Dim categoryFilter = If(_categoryCombo.SelectedIndex > 0, _categoryCombo.SelectedItem.ToString(), "")

        _filteredDtc.Clear()
        _filteredDtc.AddRange(_allDtc.Where(Function(dtc)
                                                Dim matchesSearch = String.IsNullOrEmpty(searchText) OrElse
                                                                    dtc.Code.ToLower().Contains(searchText) OrElse
                                                                    (dtc.DescriptionTR IsNot Nothing AndAlso dtc.DescriptionTR.ToLower().Contains(searchText)) OrElse
                                                                    (dtc.DescriptionEN IsNot Nothing AndAlso dtc.DescriptionEN.ToLower().Contains(searchText))

                                                Dim matchesCategory = String.IsNullOrEmpty(categoryFilter) OrElse
                                                                        categoryFilter.StartsWith(dtc.Category, StringComparison.OrdinalIgnoreCase)

                                                Return matchesSearch AndAlso matchesCategory
                                            End Function))

        RefreshGrid()
    End Sub

    Public Sub ClearAll()
        _allDtc.Clear()
        _filteredDtc.Clear()
        RefreshGrid()
    End Sub

    Public Function GetAllDtc() As List(Of DTCInfo)
        Return _allDtc
    End Function

    Private Sub DgvDTCList_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex < 0 Then Return

        Dim dtc = TryCast(_grid.Rows(e.RowIndex).Tag, DTCInfo)
        If dtc IsNot Nothing Then
            Dim detailForm As New DTCDetailForm(dtc)
            detailForm.ShowDialog(_grid.FindForm())
        End If
    End Sub

    Private Sub DgvDTCList_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
        Try
            If e.RowIndex < 0 Then Return

            Dim dtc = TryCast(_grid.Rows(e.RowIndex).Tag, DTCInfo)
            If dtc Is Nothing Then Return

            Dim row = _grid.Rows(e.RowIndex)

            Select Case dtc.Severity?.ToLower()
                Case "critical"
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(231, 76, 60)
                Case "high"
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(230, 126, 34)
                Case "medium"
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(241, 196, 15)
                Case "low"
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(46, 204, 113)
                Case Else
                    row.DefaultCellStyle.ForeColor = Color.FromArgb(220, 220, 240)
            End Select
        Catch
        End Try
    End Sub

    Private Sub TxtDTCSearch_TextChanged(sender As Object, e As EventArgs)
        ApplyFilter()
    End Sub

    Private Sub CmbDTCCategory_SelectedIndexChanged(sender As Object, e As EventArgs)
        ApplyFilter()
    End Sub

    Private Shared Function GetSeverityDisplayText(severity As String) As String
        Select Case severity?.ToLower()
            Case "critical" : Return "🔴 Kritik"
            Case "high" : Return "🟠 Yüksek"
            Case "medium" : Return "🟡 Orta"
            Case "low" : Return "🟢 Düşük"
            Case Else : Return "⚪ -"
        End Select
    End Function

    Private Shared Function GetStatusDisplayText(dtc As DTCInfo) As String
        If dtc.IsPending Then Return "⏳"
        If dtc.IsStored Then Return "💾"
        If dtc.IsPermanent Then Return "🔒"
        Return ""
    End Function
End Class
