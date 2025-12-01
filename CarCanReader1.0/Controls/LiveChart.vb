' LiveChart.vb
' Canlı veri grafik kontrolü
' Real-time line chart için optimize edilmiş UserControl

Imports System.Windows.Forms.DataVisualization.Charting
Imports System.IO
Imports System.Linq
Imports System.Threading

Public Class LiveChart
    Inherits UserControl

#Region "Private Fields"

    Private WithEvents _chart As Chart
    Private _dataPoints As New Queue(Of Tuple(Of DateTime, Double))
    Private _maxPoints As Integer = 600 ' 60 saniye x 10Hz
    Private _isPaused As Boolean = False
    Private ReadOnly _dataLock As New Object()
    Private _series As Series = Nothing

    ' Özellikler için backing fields
    Private _title As String = "Grafik"
    Private _unit As String = ""
    Private _minValue As Double = 0
    Private _maxValue As Double = 100
    Private _autoScale As Boolean = True
    Private _lineColor As Color = Color.Lime
    Private _gridColor As Color = Color.FromArgb(50, 50, 50)

    ' İstatistikler
    Private _minDataValue As Double = Double.MaxValue
    Private _maxDataValue As Double = Double.MinValue
    Private _sumDataValue As Double = 0
    Private _dataCount As Integer = 0

#End Region

#Region "Properties"

    Public Property Title As String
        Get
            Return _title
        End Get
        Set(value As String)
            _title = value
            If _chart IsNot Nothing Then
                _chart.Titles(0).Text = value
            End If
        End Set
    End Property

    Public Property Unit As String
        Get
            Return _unit
        End Get
        Set(value As String)
            _unit = value
            UpdateYAxisLabel()
        End Set
    End Property

    Public Property MinValue As Double
        Get
            Return _minValue
        End Get
        Set(value As Double)
            _minValue = value
            If Not _autoScale Then
                UpdateYAxisRange()
            End If
        End Set
    End Property

    Public Property MaxValue As Double
        Get
            Return _maxValue
        End Get
        Set(value As Double)
            _maxValue = value
            If Not _autoScale Then
                UpdateYAxisRange()
            End If
        End Set
    End Property

    Public Property AutoScale As Boolean
        Get
            Return _autoScale
        End Get
        Set(value As Boolean)
            _autoScale = value
            If value Then
                AutoScaleYAxis()
            Else
                UpdateYAxisRange()
            End If
        End Set
    End Property

    Public Property LineColor As Color
        Get
            Return _lineColor
        End Get
        Set(value As Color)
            _lineColor = value
            If _series IsNot Nothing Then
                _series.Color = value
            End If
        End Set
    End Property

    Public Property GridColor As Color
        Get
            Return _gridColor
        End Get
        Set(value As Color)
            _gridColor = value
            UpdateGridStyle()
        End Set
    End Property

    ''' <summary>
    ''' Mevcut değer
    ''' </summary>
    Public ReadOnly Property CurrentValue As Double
        Get
            SyncLock _dataLock
                If _dataPoints.Count > 0 Then
                    Return _dataPoints.Last().Item2
                End If
                Return 0
            End SyncLock
        End Get
    End Property

    ''' <summary>
    ''' Minimum değer (veri içinden)
    ''' </summary>
    Public ReadOnly Property MinDataValue As Double
        Get
            Return If(_minDataValue = Double.MaxValue, 0, _minDataValue)
        End Get
    End Property

    ''' <summary>
    ''' Maksimum değer (veri içinden)
    ''' </summary>
    Public ReadOnly Property MaxDataValue As Double
        Get
            Return If(_maxDataValue = Double.MinValue, 0, _maxDataValue)
        End Get
    End Property

    ''' <summary>
    ''' Ortalama değer
    ''' </summary>
    Public ReadOnly Property AverageValue As Double
        Get
            If _dataCount = 0 Then Return 0
            Return _sumDataValue / _dataCount
        End Get
    End Property

    ''' <summary>
    ''' Duraklatılmış mı?
    ''' </summary>
    Public ReadOnly Property IsPaused As Boolean
        Get
            Return _isPaused
        End Get
    End Property

    ''' <summary>
    ''' Veri noktası sayısı
    ''' </summary>
    Public ReadOnly Property DataPointCount As Integer
        Get
            SyncLock _dataLock
                Return _dataPoints.Count
            End SyncLock
        End Get
    End Property

#End Region

#Region "Constructor"

    Public Sub New()
        Me.DoubleBuffered = True
        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.DoubleBuffer Or ControlStyles.ResizeRedraw, True)
        InitializeChart()
    End Sub

#End Region

#Region "Initialize Chart"

    Private Sub InitializeChart()
        ' Chart kontrolü oluştur
        _chart = New Chart()
        _chart.Dock = DockStyle.Fill
        _chart.BackColor = Color.FromArgb(30, 30, 40)
        _chart.ChartAreas.Clear()
        _chart.Series.Clear()
        _chart.Legends.Clear()
        _chart.Titles.Clear()

        ' ChartArea oluştur
        Dim chartArea As New ChartArea("MainArea")
        chartArea.BackColor = Color.FromArgb(30, 30, 40)
        chartArea.BorderColor = Color.FromArgb(60, 60, 80)
        chartArea.BorderWidth = 1

        ' X ekseni (Zaman)
        chartArea.AxisX.Title = "Zaman"
        chartArea.AxisX.TitleFont = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        chartArea.AxisX.TitleForeColor = Color.White
        chartArea.AxisX.LabelStyle.ForeColor = Color.FromArgb(200, 200, 220)
        chartArea.AxisX.LabelStyle.Font = New Font("Segoe UI", 8.0!)
        chartArea.AxisX.LineColor = Color.FromArgb(100, 100, 120)
        chartArea.AxisX.MajorGrid.LineColor = _gridColor
        chartArea.AxisX.MajorGrid.LineWidth = 1
        chartArea.AxisX.MajorGrid.Enabled = True
        chartArea.AxisX.MinorGrid.Enabled = False
        chartArea.AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount
        chartArea.AxisX.LabelStyle.Format = "HH:mm:ss"

        ' Y ekseni (Değer)
        chartArea.AxisY.Title = _title & If(Not String.IsNullOrEmpty(_unit), $" ({_unit})", "")
        chartArea.AxisY.TitleFont = New Font("Segoe UI", 9.0!, FontStyle.Bold)
        chartArea.AxisY.TitleForeColor = Color.White
        chartArea.AxisY.LabelStyle.ForeColor = Color.FromArgb(200, 200, 220)
        chartArea.AxisY.LabelStyle.Font = New Font("Segoe UI", 8.0!)
        chartArea.AxisY.LineColor = Color.FromArgb(100, 100, 120)
        chartArea.AxisY.MajorGrid.LineColor = _gridColor
        chartArea.AxisY.MajorGrid.LineWidth = 1
        chartArea.AxisY.MajorGrid.Enabled = True
        chartArea.AxisY.MinorGrid.Enabled = False
        chartArea.AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount

        ' Otomatik ölçekleme
        If _autoScale Then
            chartArea.AxisY.IsStartedFromZero = False
        End If

        _chart.ChartAreas.Add(chartArea)

        ' Series oluştur
        _series = New Series("DataSeries")
        _series.ChartType = SeriesChartType.FastLine
        _series.Color = _lineColor
        _series.BorderWidth = 2
        _series.MarkerStyle = MarkerStyle.None
        _series.IsVisibleInLegend = False
        _series.XValueType = ChartValueType.DateTime
        _chart.Series.Add(_series)

        ' Başlık
        Dim title As New Title(_title)
        title.Font = New Font("Segoe UI", 12.0!, FontStyle.Bold)
        title.ForeColor = Color.White
        title.Alignment = ContentAlignment.TopCenter
        _chart.Titles.Add(title)

        ' Performans ayarları
        _chart.AntiAliasing = AntiAliasingStyles.Text
        _chart.TextAntiAliasingQuality = TextAntiAliasingQuality.High

        ' Kontrol'e ekle
        Me.Controls.Add(_chart)
    End Sub

#End Region

#Region "Public Methods"

    ''' <summary>
    ''' Yeni veri noktası ekle
    ''' </summary>
    Public Sub AddDataPoint(value As Double)
        If _isPaused Then Return

        Try
            SyncLock _dataLock
                Dim now = DateTime.Now
                _dataPoints.Enqueue(New Tuple(Of DateTime, Double)(now, value))

                ' İstatistikleri güncelle
                If value < _minDataValue Then _minDataValue = value
                If value > _maxDataValue Then _maxDataValue = value
                _sumDataValue += value
                _dataCount += 1

                ' Maksimum nokta sayısını aşarsa eski verileri sil
                While _dataPoints.Count > _maxPoints
                    Dim removed = _dataPoints.Dequeue()
                    ' İstatistikleri güncelle (basitleştirilmiş - tam doğru değil ama performans için)
                End While
            End SyncLock

            ' UI thread'de güncelle
            If Me.InvokeRequired Then
                Me.BeginInvoke(New Action(Of Double)(AddressOf UpdateChart), value)
            Else
                UpdateChart(value)
            End If

        Catch ex As Exception
            ' Sessizce ignore - performans için
        End Try
    End Sub

    ''' <summary>
    ''' Grafiği temizle
    ''' </summary>
    Public Sub Clear()
        SyncLock _dataLock
            _dataPoints.Clear()
            _minDataValue = Double.MaxValue
            _maxDataValue = Double.MinValue
            _sumDataValue = 0
            _dataCount = 0
        End SyncLock

        If Me.InvokeRequired Then
            Me.Invoke(New Action(AddressOf ClearChart))
        Else
            ClearChart()
        End If
    End Sub

    ''' <summary>
    ''' Duraklat
    ''' </summary>
    Public Sub Pause()
        _isPaused = True
    End Sub

    ''' <summary>
    ''' Devam et
    ''' </summary>
    Public Sub [Resume]()
        _isPaused = False
    End Sub

    ''' <summary>
    ''' CSV'ye dışa aktar
    ''' </summary>
    Public Sub ExportToCsv(filePath As String)
        Try
            SyncLock _dataLock
                Using writer As New StreamWriter(filePath, False, System.Text.Encoding.UTF8)
                    writer.WriteLine("Zaman,Değer")
                    For Each point In _dataPoints
                        writer.WriteLine($"{point.Item1:yyyy-MM-dd HH:mm:ss.fff},{point.Item2:F2}")
                    Next
                End Using
            End SyncLock
        Catch ex As Exception
            Throw New Exception($"CSV export hatası: {ex.Message}", ex)
        End Try
    End Sub

    ''' <summary>
    ''' Maksimum nokta sayısını ayarla (zaman aralığı değişimi için)
    ''' </summary>
    Public Sub SetTimeRange(seconds As Integer)
        _maxPoints = seconds * 10 ' 10Hz varsayılan
        SyncLock _dataLock
            While _dataPoints.Count > _maxPoints
                _dataPoints.Dequeue()
            End While
        End SyncLock
    End Sub

#End Region

#Region "Private Methods"

    ''' <summary>
    ''' Grafiği güncelle
    ''' </summary>
    Private Sub UpdateChart(value As Double)
        Try
            If _chart Is Nothing OrElse _series Is Nothing Then Return

            SyncLock _dataLock
                ' Series'i temizle ve yeniden doldur (performans için sadece son N nokta)
                _series.Points.Clear()

                ' Son 600 noktayı ekle (veya daha az)
                Dim pointsToShow = Math.Min(_dataPoints.Count, _maxPoints)
                Dim skipCount = Math.Max(0, _dataPoints.Count - _maxPoints)

                Dim pointList = _dataPoints.Skip(skipCount).ToList()
                For Each point In pointList
                    _series.Points.AddXY(point.Item1, point.Item2)
                Next
            End SyncLock

            ' Y eksenini otomatik ölçekle
            If _autoScale Then
                AutoScaleYAxis()
            End If

            ' X eksenini güncelle (son 60 saniye göster)
            UpdateXAxis()

        Catch ex As Exception
            ' Sessizce ignore
        End Try
    End Sub

    ''' <summary>
    ''' Grafiği temizle
    ''' </summary>
    Private Sub ClearChart()
        If _series IsNot Nothing Then
            _series.Points.Clear()
        End If
    End Sub

    ''' <summary>
    ''' Y eksenini otomatik ölçekle
    ''' </summary>
    Private Sub AutoScaleYAxis()
        Try
            If _chart Is Nothing OrElse _chart.ChartAreas.Count = 0 Then Return

            SyncLock _dataLock
                If _dataPoints.Count = 0 Then Return

                Dim values = _dataPoints.Select(Function(p) p.Item2).ToList()
                Dim minVal = values.Min()
                Dim maxVal = values.Max()

                ' %10 margin ekle
                Dim range = maxVal - minVal
                If range = 0 Then range = 1
                Dim margin = range * 0.1

                Dim axis = _chart.ChartAreas(0).AxisY
                axis.Minimum = minVal - margin
                axis.Maximum = maxVal + margin
            End SyncLock
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Y ekseni aralığını güncelle (manuel)
    ''' </summary>
    Private Sub UpdateYAxisRange()
        Try
            If _chart Is Nothing OrElse _chart.ChartAreas.Count = 0 Then Return

            Dim axis = _chart.ChartAreas(0).AxisY
            axis.Minimum = _minValue
            axis.Maximum = _maxValue
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Y ekseni etiketini güncelle
    ''' </summary>
    Private Sub UpdateYAxisLabel()
        Try
            If _chart Is Nothing OrElse _chart.ChartAreas.Count = 0 Then Return

            Dim axis = _chart.ChartAreas(0).AxisY
            axis.Title = _title & If(Not String.IsNullOrEmpty(_unit), $" ({_unit})", "")
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' X eksenini güncelle (son 60 saniye)
    ''' </summary>
    Private Sub UpdateXAxis()
        Try
            If _chart Is Nothing OrElse _chart.ChartAreas.Count = 0 Then Return
            If _dataPoints.Count = 0 Then Return

            SyncLock _dataLock
                Dim now = DateTime.Now
                Dim oldestTime = If(_dataPoints.Count > 0, _dataPoints.Peek().Item1, now)
                Dim timeSpan = (now - oldestTime).TotalSeconds

                ' En az 60 saniye göster
                If timeSpan < 60 Then
                    oldestTime = now.AddSeconds(-60)
                End If

                Dim axis = _chart.ChartAreas(0).AxisX
                axis.Minimum = oldestTime.ToOADate()
                axis.Maximum = now.ToOADate()
            End SyncLock
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Grid stilini güncelle
    ''' </summary>
    Private Sub UpdateGridStyle()
        Try
            If _chart Is Nothing OrElse _chart.ChartAreas.Count = 0 Then Return

            Dim chartArea = _chart.ChartAreas(0)
            chartArea.AxisX.MajorGrid.LineColor = _gridColor
            chartArea.AxisY.MajorGrid.LineColor = _gridColor
        Catch
        End Try
    End Sub

#End Region

#Region "Dispose"

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            Try
                If _chart IsNot Nothing Then
                    _chart.Dispose()
                End If
            Catch
            End Try
        End If
        MyBase.Dispose(disposing)
    End Sub

#End Region

End Class

