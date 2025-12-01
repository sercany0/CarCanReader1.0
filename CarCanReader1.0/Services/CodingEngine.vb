' CodingEngine.vb
' Gizli özellik (coding) işleme servisi
' Tüm coding/hidden-feature mantığını içerir
'
' ÖNEMLİ: Bu sınıf UI kodu İÇERMEZ.
' MainForm tarafından kullanılır:
'   - ApplyCodingCommand
'   - SaveCodingCommand
'   - btnCodingOn_Click
'   - btnCodingOff_Click
'   - btnCodeSend_Click
'
' Kullanım:
'   Dim engine As New CodingEngine()
'   Dim commands = engine.LoadAvailableCommands(commandsData, brand, model, moduleName)
'   Dim frame = engine.GetCommandFrame(commandsData, brand, model, moduleName, cmdName)
'   Dim preview = engine.BuildPreview(frame)

Imports Newtonsoft.Json.Linq

Namespace Services

    ''' <summary>
    ''' Gizli özellik (coding) işleme motoru
    ''' UI kodu içermez, saf mantık sınıfıdır
    ''' </summary>
    Public Class CodingEngine

#Region "Sabitler"

        ' Varsayılan ON değeri
        Private Const ON_VALUE As Byte = &H1

        ' Varsayılan OFF değeri
        Private Const OFF_VALUE As Byte = &H0

        ' Maksimum frame uzunluğu
        Private Const MAX_FRAME_LENGTH As Integer = 8

#End Region

#Region "LoadAvailableCommands"

        ''' <summary>
        ''' Belirtilen brand/model/module altındaki komut listesini döndürür
        ''' </summary>
        ''' <param name="commandsData">JSON komut verisi (JObject)</param>
        ''' <param name="brand">Marka adı</param>
        ''' <param name="model">Model adı</param>
        ''' <param name="moduleName">Modül adı</param>
        ''' <returns>Komut adları listesi, hata durumunda boş liste</returns>
        Public Function LoadAvailableCommands(commandsData As JObject,
                                               brand As String,
                                               model As String,
                                               moduleName As String) As List(Of String)
            Dim result As New List(Of String)()

            Try
                ' Null kontrolleri
                If commandsData Is Nothing Then Return result
                If String.IsNullOrWhiteSpace(brand) Then Return result
                If String.IsNullOrWhiteSpace(model) Then Return result
                If String.IsNullOrWhiteSpace(moduleName) Then Return result

                ' Hiyerarşiyi takip et
                Dim brandToken = commandsData(brand)
                If brandToken Is Nothing Then Return result

                Dim modelToken = brandToken(model)
                If modelToken Is Nothing Then Return result

                Dim moduleToken = modelToken(moduleName)
                If moduleToken Is Nothing Then Return result

                ' JObject olarak al
                Dim moduleObj = TryCast(moduleToken, JObject)
                If moduleObj Is Nothing Then Return result

                ' Tüm komut adlarını topla
                For Each prop As JProperty In moduleObj.Properties()
                    result.Add(prop.Name)
                Next

            Catch
                ' Hata durumunda boş liste döndür
                result.Clear()
            End Try

            Return result
        End Function

#End Region

#Region "GetCommandFrame"

        ''' <summary>
        ''' Belirtilen komutun frame verisini Byte() olarak döndürür
        ''' JSON'da frame şu formatlarda olabilir:
        '''   - Hex string: "11 22 33 44"
        '''   - Integer array: [17, 34, 51, 68]
        '''   - Direkt string: "t1238AABBCCDD"
        ''' </summary>
        ''' <param name="commandsData">JSON komut verisi</param>
        ''' <param name="brand">Marka adı</param>
        ''' <param name="model">Model adı</param>
        ''' <param name="moduleName">Modül adı</param>
        ''' <param name="commandName">Komut adı</param>
        ''' <returns>Frame byte dizisi, hata durumunda boş dizi</returns>
        Public Function GetCommandFrame(commandsData As JObject,
                                          brand As String,
                                          model As String,
                                          moduleName As String,
                                          commandName As String) As Byte()
            Try
                ' Null kontrolleri
                If commandsData Is Nothing Then Return New Byte() {}
                If String.IsNullOrWhiteSpace(brand) Then Return New Byte() {}
                If String.IsNullOrWhiteSpace(model) Then Return New Byte() {}
                If String.IsNullOrWhiteSpace(moduleName) Then Return New Byte() {}
                If String.IsNullOrWhiteSpace(commandName) Then Return New Byte() {}

                ' Komutu bul
                Dim cmdToken = commandsData(brand)?(model)?(moduleName)?(commandName)
                If cmdToken Is Nothing Then Return New Byte() {}

                ' Token tipine göre işle
                Return ParseCommandToken(cmdToken)

            Catch
                Return New Byte() {}
            End Try
        End Function

        ''' <summary>
        ''' JToken'ı Byte() dizisine çevirir
        ''' </summary>
        Private Function ParseCommandToken(token As JToken) As Byte()
            Try
                If token Is Nothing Then Return New Byte() {}

                ' String ise
                If token.Type = JTokenType.String Then
                    Dim strValue As String = token.ToString()
                    Return ParseFrameString(strValue)
                End If

                ' Array ise (integer array)
                If token.Type = JTokenType.Array Then
                    Dim arr = CType(token, JArray)
                    Dim bytes(arr.Count - 1) As Byte

                    For i As Integer = 0 To arr.Count - 1
                        bytes(i) = CByte(CInt(arr(i)))
                    Next

                    Return bytes
                End If

                ' Object ise (toggle command gibi)
                If token.Type = JTokenType.Object Then
                    Dim obj = CType(token, JObject)

                    ' "on" frame varsa onu kullan (varsayılan)
                    Dim onFrame = obj("on")
                    If onFrame IsNot Nothing Then
                        Return ParseCommandToken(onFrame)
                    End If

                    ' "data" varsa onu kullan
                    Dim dataFrame = obj("data")
                    If dataFrame IsNot Nothing Then
                        Return ParseCommandToken(dataFrame)
                    End If
                End If

                Return New Byte() {}

            Catch
                Return New Byte() {}
            End Try
        End Function

        ''' <summary>
        ''' Frame string'i Byte() dizisine çevirir
        ''' Desteklenen formatlar:
        '''   - "11 22 33 44" (boşluklu hex)
        '''   - "11223344" (bitişik hex)
        '''   - "t1238AABBCCDD" (SLCAN format)
        ''' </summary>
        Private Function ParseFrameString(frameStr As String) As Byte()
            Try
                If String.IsNullOrWhiteSpace(frameStr) Then Return New Byte() {}

                frameStr = frameStr.Trim()

                ' SLCAN formatı mı kontrol et (t ile başlıyor)
                If frameStr.StartsWith("t", StringComparison.OrdinalIgnoreCase) Then
                    Return ParseSlcanFrame(frameStr)
                End If

                ' Boşluk içeriyorsa boşluklarla ayrılmış hex
                If frameStr.Contains(" ") Then
                    Return SafeParseHexArray(frameStr)
                End If

                ' Bitişik hex string
                Return ParseContinuousHex(frameStr)

            Catch
                Return New Byte() {}
            End Try
        End Function

        ''' <summary>
        ''' SLCAN frame string'inden data kısmını çıkarır
        ''' Format: tIDDLCDATA (örn: t1238AABBCCDDEEFF)
        ''' </summary>
        Private Function ParseSlcanFrame(slcanStr As String) As Byte()
            Try
                If slcanStr.Length < 5 Then Return New Byte() {}

                ' t + 3 ID + 1 DLC = 5. karakterden sonrası data
                Dim dataHex As String = slcanStr.Substring(5)
                Return ParseContinuousHex(dataHex)

            Catch
                Return New Byte() {}
            End Try
        End Function

        ''' <summary>
        ''' Bitişik hex string'i Byte() dizisine çevirir
        ''' </summary>
        Private Function ParseContinuousHex(hexStr As String) As Byte()
            Try
                If String.IsNullOrWhiteSpace(hexStr) Then Return New Byte() {}

                ' Tek sayıda karakter varsa başa 0 ekle
                If hexStr.Length Mod 2 <> 0 Then
                    hexStr = "0" & hexStr
                End If

                Dim byteCount As Integer = hexStr.Length \ 2
                Dim bytes(byteCount - 1) As Byte

                For i As Integer = 0 To byteCount - 1
                    Dim hexPair As String = hexStr.Substring(i * 2, 2)
                    Dim byteValue As Integer

                    If Integer.TryParse(hexPair, Globalization.NumberStyles.HexNumber, Nothing, byteValue) Then
                        bytes(i) = CByte(byteValue)
                    Else
                        bytes(i) = 0
                    End If
                Next

                Return bytes

            Catch
                Return New Byte() {}
            End Try
        End Function

#End Region

#Region "BuildPreview"

        ''' <summary>
        ''' Frame için gelişmiş önizleme metni oluşturur
        ''' Görsel tablo formatı ve byte vurgulama desteği
        ''' </summary>
        ''' <param name="frame">Frame byte dizisi</param>
        ''' <param name="highlightIndex">Vurgulanacak byte indeksi (-1 = yok)</param>
        ''' <returns>Formatlanmış önizleme metni</returns>
        Public Function BuildPreview(frame As Byte(), Optional highlightIndex As Integer = -1) As String
            Try
                If frame Is Nothing OrElse frame.Length = 0 Then
                    Return "╔════════════════════════════╗" & vbCrLf &
                           "║  [!] Frame verisi boş      ║" & vbCrLf &
                           "╚════════════════════════════╝"
                End If

                Dim sb As New System.Text.StringBuilder()

                ' Başlık çizgisi
                sb.AppendLine("╔════════════════════════════════════════════╗")
                sb.AppendLine("║           FRAME DATA PREVIEW               ║")
                sb.AppendLine("╠════════════════════════════════════════════╣")

                ' Data bytes tablo satırı
                sb.Append("║  DATA: ")
                For i As Integer = 0 To frame.Length - 1
                    If i = highlightIndex Then
                        sb.Append($"[{frame(i):X2}]")
                    Else
                        sb.Append($" {frame(i):X2} ")
                    End If
                Next
                ' Padding
                Dim padding As Integer = 36 - (frame.Length * 4)
                If padding > 0 Then sb.Append(New String(" "c, padding))
                sb.AppendLine("║")

                ' Byte index satırı
                sb.Append("║  IDX:  ")
                For i As Integer = 0 To frame.Length - 1
                    If i = highlightIndex Then
                        sb.Append($"[{i,2}]")
                    Else
                        sb.Append($" {i,2} ")
                    End If
                Next
                If padding > 0 Then sb.Append(New String(" "c, padding))
                sb.AppendLine("║")

                ' Highlight bilgisi
                If highlightIndex >= 0 AndAlso highlightIndex < frame.Length Then
                    sb.AppendLine("╠════════════════════════════════════════════╣")
                    sb.AppendLine($"║  [*] MODIFIED BYTE[{highlightIndex}] = 0x{frame(highlightIndex):X2} ({frame(highlightIndex)})        ║")
                End If

                ' Alt çizgi
                sb.AppendLine("╚════════════════════════════════════════════╝")

                Return sb.ToString()

            Catch ex As Exception
                Return $"[!] Önizleme hatası: {ex.Message}"
            End Try
        End Function

        ''' <summary>
        ''' Basit önizleme formatı (tek satır)
        ''' </summary>
        Public Function BuildSimplePreview(frame As Byte()) As String
            Try
                If frame Is Nothing OrElse frame.Length = 0 Then Return "(boş)"
                Return BitConverter.ToString(frame).Replace("-", " ")
            Catch
                Return "(hata)"
            End Try
        End Function

        ''' <summary>
        ''' ID bilgisi ile birlikte gelişmiş önizleme oluşturur
        ''' </summary>
        Public Function BuildPreviewWithId(canId As String, frame As Byte(), Optional highlightIndex As Integer = -1) As String
            Try
                Dim sb As New System.Text.StringBuilder()

                sb.AppendLine("╔════════════════════════════════════════════╗")
                sb.AppendLine($"║  CAN ID: 0x{canId.ToUpper().PadLeft(3, "0"c)}                             ║")
                sb.AppendLine("╠════════════════════════════════════════════╣")

                ' Data bytes
                sb.Append("║  DATA: ")
                If frame IsNot Nothing AndAlso frame.Length > 0 Then
                    For i As Integer = 0 To frame.Length - 1
                        If i = highlightIndex Then
                            sb.Append($"[{frame(i):X2}]")
                        Else
                            sb.Append($" {frame(i):X2} ")
                        End If
                    Next
                Else
                    sb.Append("(boş)")
                End If
                sb.AppendLine()

                If highlightIndex >= 0 AndAlso frame IsNot Nothing AndAlso highlightIndex < frame.Length Then
                    sb.AppendLine($"║  >>> Byte[{highlightIndex}] = 0x{frame(highlightIndex):X2}                    ║")
                End If

                sb.AppendLine("╚════════════════════════════════════════════╝")

                Return sb.ToString()

            Catch
                Return "[!] Önizleme oluşturulamadı"
            End Try
        End Function

        ''' <summary>
        ''' Toggle komutu için özel önizleme (ON/OFF durumlarını gösterir)
        ''' </summary>
        Public Function BuildTogglePreview(canId As String, onFrame As Byte(), offFrame As Byte(), controlByteIndex As Integer) As String
            Try
                Dim sb As New System.Text.StringBuilder()

                sb.AppendLine("╔═══════════════════════════════════════════════════╗")
                sb.AppendLine("║           TOGGLE COMMAND PREVIEW                  ║")
                sb.AppendLine("╠═══════════════════════════════════════════════════╣")
                sb.AppendLine($"║  CAN ID: 0x{canId.ToUpper().PadLeft(3, "0"c)}                                     ║")
                sb.AppendLine($"║  Control Byte: [{controlByteIndex}]                               ║")
                sb.AppendLine("╠═══════════════════════════════════════════════════╣")

                ' ON Frame
                sb.Append("║  [ON]  : ")
                If onFrame IsNot Nothing Then
                    For i As Integer = 0 To onFrame.Length - 1
                        If i = controlByteIndex Then
                            sb.Append($">{onFrame(i):X2}<")
                        Else
                            sb.Append($" {onFrame(i):X2} ")
                        End If
                    Next
                End If
                sb.AppendLine()

                ' OFF Frame
                sb.Append("║  [OFF] : ")
                If offFrame IsNot Nothing Then
                    For i As Integer = 0 To offFrame.Length - 1
                        If i = controlByteIndex Then
                            sb.Append($">{offFrame(i):X2}<")
                        Else
                            sb.Append($" {offFrame(i):X2} ")
                        End If
                    Next
                End If
                sb.AppendLine()

                sb.AppendLine("╠═══════════════════════════════════════════════════╣")
                If onFrame IsNot Nothing AndAlso offFrame IsNot Nothing AndAlso
                   controlByteIndex >= 0 AndAlso controlByteIndex < onFrame.Length Then
                    sb.AppendLine($"║  ON Value:  0x{onFrame(controlByteIndex):X2} ({onFrame(controlByteIndex)})                           ║")
                    sb.AppendLine($"║  OFF Value: 0x{offFrame(controlByteIndex):X2} ({offFrame(controlByteIndex)})                           ║")
                End If
                sb.AppendLine("╚═══════════════════════════════════════════════════╝")

                Return sb.ToString()

            Catch
                Return "[!] Toggle önizleme oluşturulamadı"
            End Try
        End Function

#End Region

#Region "BuildModifiedFrame"

        ''' <summary>
        ''' Orijinal frame'de belirtilen byte'ı değiştirerek YENİ bir dizi döndürür
        ''' Orijinal dizi değiştirilmez
        ''' </summary>
        ''' <param name="originalFrame">Orijinal frame</param>
        ''' <param name="byteIndex">Değiştirilecek byte indeksi</param>
        ''' <param name="newHexValue">Yeni değer (hex string, örn: "FF")</param>
        ''' <returns>Değiştirilmiş yeni frame, hata durumunda orijinalin kopyası</returns>
        Public Function BuildModifiedFrame(originalFrame As Byte(),
                                            byteIndex As Integer,
                                            newHexValue As String) As Byte()
            Try
                ' Null kontrolü
                If originalFrame Is Nothing OrElse originalFrame.Length = 0 Then
                    Return New Byte() {}
                End If

                ' İndeks kontrolü
                If byteIndex < 0 OrElse byteIndex >= originalFrame.Length Then
                    ' Geçersiz indeks, orijinalin kopyasını döndür
                    Return CloneByteArray(originalFrame)
                End If

                ' Hex değeri parse et
                Dim newValue As Byte = ParseHexByte(newHexValue)

                ' Yeni dizi oluştur ve kopyala
                Dim newFrame As Byte() = CloneByteArray(originalFrame)

                ' Byte'ı değiştir
                newFrame(byteIndex) = newValue

                Return newFrame

            Catch
                ' Hata durumunda orijinalin kopyasını döndür
                If originalFrame IsNot Nothing Then
                    Return CloneByteArray(originalFrame)
                End If
                Return New Byte() {}
            End Try
        End Function

        ''' <summary>
        ''' Hex string'i tek byte'a çevirir
        ''' </summary>
        Private Function ParseHexByte(hexStr As String) As Byte
            Try
                If String.IsNullOrWhiteSpace(hexStr) Then Return 0

                hexStr = hexStr.Trim().ToUpper()

                ' 0x prefix varsa kaldır
                If hexStr.StartsWith("0X") Then
                    hexStr = hexStr.Substring(2)
                End If

                Dim value As Integer
                If Integer.TryParse(hexStr, Globalization.NumberStyles.HexNumber, Nothing, value) Then
                    ' Byte aralığında tut
                    If value < 0 Then value = 0
                    If value > 255 Then value = 255
                    Return CByte(value)
                End If

                Return 0

            Catch
                Return 0
            End Try
        End Function

#End Region

#Region "BuildOnOffFrame"

        ''' <summary>
        ''' ON/OFF toggle frame oluşturur
        ''' isOn=True → byteIndex = 0x01
        ''' isOn=False → byteIndex = 0x00
        ''' </summary>
        ''' <param name="originalFrame">Orijinal frame</param>
        ''' <param name="byteIndex">Toggle byte indeksi</param>
        ''' <param name="isOn">True=ON, False=OFF</param>
        ''' <returns>Değiştirilmiş yeni frame</returns>
        Public Function BuildOnOffFrame(originalFrame As Byte(),
                                          byteIndex As Integer,
                                          isOn As Boolean) As Byte()
            Try
                ' Null kontrolü
                If originalFrame Is Nothing OrElse originalFrame.Length = 0 Then
                    Return New Byte() {}
                End If

                ' İndeks kontrolü
                If byteIndex < 0 OrElse byteIndex >= originalFrame.Length Then
                    Return CloneByteArray(originalFrame)
                End If

                ' Yeni dizi oluştur
                Dim newFrame As Byte() = CloneByteArray(originalFrame)

                ' ON veya OFF değerini ayarla
                If isOn Then
                    newFrame(byteIndex) = ON_VALUE
                Else
                    newFrame(byteIndex) = OFF_VALUE
                End If

                Return newFrame

            Catch
                If originalFrame IsNot Nothing Then
                    Return CloneByteArray(originalFrame)
                End If
                Return New Byte() {}
            End Try
        End Function

        ''' <summary>
        ''' Özel ON/OFF değerleri ile toggle frame oluşturur
        ''' </summary>
        Public Function BuildOnOffFrameCustom(originalFrame As Byte(),
                                               byteIndex As Integer,
                                               isOn As Boolean,
                                               onValue As Byte,
                                               offValue As Byte) As Byte()
            Try
                If originalFrame Is Nothing OrElse originalFrame.Length = 0 Then
                    Return New Byte() {}
                End If

                If byteIndex < 0 OrElse byteIndex >= originalFrame.Length Then
                    Return CloneByteArray(originalFrame)
                End If

                Dim newFrame As Byte() = CloneByteArray(originalFrame)

                If isOn Then
                    newFrame(byteIndex) = onValue
                Else
                    newFrame(byteIndex) = offValue
                End If

                Return newFrame

            Catch
                If originalFrame IsNot Nothing Then
                    Return CloneByteArray(originalFrame)
                End If
                Return New Byte() {}
            End Try
        End Function

#End Region

#Region "SafeParseHexArray"

        ''' <summary>
        ''' Boşlukla ayrılmış hex string'i Byte() dizisine çevirir
        ''' Geçersiz token'ları atlar
        ''' Her zaman geçerli bir Byte() döndürür (boş olabilir)
        ''' </summary>
        ''' <param name="hexString">Hex string (örn: "11 22 33 AA BB")</param>
        ''' <returns>Byte dizisi</returns>
        Public Function SafeParseHexArray(hexString As String) As Byte()
            Try
                If String.IsNullOrWhiteSpace(hexString) Then
                    Return New Byte() {}
                End If

                ' Ayırıcıları normalize et
                hexString = hexString.Replace(",", " ").Replace(";", " ").Replace("-", " ")

                ' Token'lara ayır
                Dim tokens As String() = hexString.Split(New Char() {" "c}, StringSplitOptions.RemoveEmptyEntries)

                Dim byteList As New List(Of Byte)()

                For Each token In tokens
                    Dim cleanToken As String = token.Trim().ToUpper()

                    ' 0x prefix varsa kaldır
                    If cleanToken.StartsWith("0X") Then
                        cleanToken = cleanToken.Substring(2)
                    End If

                    ' Boş token atla
                    If String.IsNullOrWhiteSpace(cleanToken) Then Continue For

                    ' Parse et
                    Dim value As Integer
                    If Integer.TryParse(cleanToken, Globalization.NumberStyles.HexNumber, Nothing, value) Then
                        ' Byte aralığında mı kontrol et
                        If value >= 0 AndAlso value <= 255 Then
                            byteList.Add(CByte(value))
                        End If
                    End If
                    ' Geçersiz token sessizce atlanır
                Next

                Return byteList.ToArray()

            Catch
                Return New Byte() {}
            End Try
        End Function

#End Region

#Region "Yardımcı Metodlar"

        ''' <summary>
        ''' Byte dizisini kopyalar
        ''' </summary>
        Private Function CloneByteArray(source As Byte()) As Byte()
            If source Is Nothing Then Return New Byte() {}

            Dim copy(source.Length - 1) As Byte
            Array.Copy(source, copy, source.Length)
            Return copy
        End Function

        ''' <summary>
        ''' Byte dizisini hex string'e çevirir
        ''' </summary>
        Public Function BytesToHexString(data As Byte(), Optional separator As String = " ") As String
            Try
                If data Is Nothing OrElse data.Length = 0 Then Return ""

                Dim sb As New System.Text.StringBuilder()
                For i As Integer = 0 To data.Length - 1
                    If i > 0 AndAlso Not String.IsNullOrEmpty(separator) Then
                        sb.Append(separator)
                    End If
                    sb.Append(data(i).ToString("X2"))
                Next
                Return sb.ToString()

            Catch
                Return ""
            End Try
        End Function

        ''' <summary>
        ''' SLCAN frame string oluşturur
        ''' </summary>
        Public Function BuildSlcanFrame(canId As String, data As Byte()) As String
            Try
                If String.IsNullOrWhiteSpace(canId) Then Return ""
                If data Is Nothing Then data = New Byte() {}

                ' ID'yi 3 karaktere tamamla
                canId = canId.ToUpper().PadLeft(3, "0"c)

                ' DLC
                Dim dlc As Integer = data.Length

                ' Data hex
                Dim dataHex As String = BytesToHexString(data, "")

                ' SLCAN format: tIDDLCDATA
                Return "t" & canId & dlc.ToString() & dataHex

            Catch
                Return ""
            End Try
        End Function

        ''' <summary>
        ''' Hex string'in geçerli olup olmadığını kontrol eder
        ''' </summary>
        Public Function IsValidHexString(hexStr As String) As Boolean
            Try
                If String.IsNullOrWhiteSpace(hexStr) Then Return False

                hexStr = hexStr.Replace(" ", "").Replace(",", "").ToUpper()

                For Each c As Char In hexStr
                    If Not ((c >= "0"c AndAlso c <= "9"c) OrElse (c >= "A"c AndAlso c <= "F"c)) Then
                        Return False
                    End If
                Next

                Return True

            Catch
                Return False
            End Try
        End Function

#End Region

#Region "Toggle Komut Desteği"

        ''' <summary>
        ''' JSON'dan toggle komutun ON frame'ini alır
        ''' </summary>
        Public Function GetToggleOnFrame(commandsData As JObject,
                                          brand As String,
                                          model As String,
                                          moduleName As String,
                                          commandName As String) As Byte()
            Try
                Dim token = commandsData(brand)?(model)?(moduleName)?(commandName)
                If token Is Nothing Then Return New Byte() {}

                If token.Type = JTokenType.Object Then
                    Dim obj = CType(token, JObject)
                    Dim onFrame = obj("on")
                    If onFrame IsNot Nothing Then
                        Return ParseCommandToken(onFrame)
                    End If
                End If

                Return New Byte() {}

            Catch
                Return New Byte() {}
            End Try
        End Function

        ''' <summary>
        ''' JSON'dan toggle komutun OFF frame'ini alır
        ''' </summary>
        Public Function GetToggleOffFrame(commandsData As JObject,
                                           brand As String,
                                           model As String,
                                           moduleName As String,
                                           commandName As String) As Byte()
            Try
                Dim token = commandsData(brand)?(model)?(moduleName)?(commandName)
                If token Is Nothing Then Return New Byte() {}

                If token.Type = JTokenType.Object Then
                    Dim obj = CType(token, JObject)
                    Dim offFrame = obj("off")
                    If offFrame IsNot Nothing Then
                        Return ParseCommandToken(offFrame)
                    End If
                End If

                Return New Byte() {}

            Catch
                Return New Byte() {}
            End Try
        End Function

        ''' <summary>
        ''' Komutun toggle tipinde olup olmadığını kontrol eder
        ''' </summary>
        Public Function IsToggleCommand(commandsData As JObject,
                                          brand As String,
                                          model As String,
                                          moduleName As String,
                                          commandName As String) As Boolean
            Try
                Dim token = commandsData(brand)?(model)?(moduleName)?(commandName)
                If token Is Nothing Then Return False

                If token.Type = JTokenType.Object Then
                    Dim obj = CType(token, JObject)
                    Dim typeVal = obj("type")?.ToString()
                    Return typeVal = "toggle"
                End If

                Return False

            Catch
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Toggle komutun byte indeksini alır
        ''' </summary>
        Public Function GetToggleByteIndex(commandsData As JObject,
                                            brand As String,
                                            model As String,
                                            moduleName As String,
                                            commandName As String) As Integer
            Try
                Dim token = commandsData(brand)?(model)?(moduleName)?(commandName)
                If token Is Nothing Then Return -1

                If token.Type = JTokenType.Object Then
                    Dim obj = CType(token, JObject)
                    Dim byteToken = obj("byte")
                    If byteToken IsNot Nothing Then
                        Return CInt(byteToken)
                    End If
                End If

                Return -1

            Catch
                Return -1
            End Try
        End Function

#End Region

    End Class

End Namespace

