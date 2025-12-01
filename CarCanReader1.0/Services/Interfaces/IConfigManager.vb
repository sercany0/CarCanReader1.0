' IConfigManager.vb
' Uygulama konfigürasyon yönetimi için arayüz taslağı

Namespace Services
    ''' <summary>
    ''' ConfigManager'ın DI dostu arayüzü; mevcut event ve temel işlemleri soyutlar.
    ''' </summary>
    Public Interface IConfigManager
        Event OnConfigChanged(section As String)
        Event OnConfigSaved()
        Event OnConfigLoaded()
        Event OnConfigReset()

        ReadOnly Property Serial As ConfigManager.SerialConfig
        ReadOnly Property OBD As ConfigManager.OBDConfig
        ReadOnly Property UI As ConfigManager.UIConfig
        ReadOnly Property Paths As ConfigManager.PathsConfig
        ReadOnly Property Advanced As ConfigManager.AdvancedConfig
        ReadOnly Property Company As ConfigManager.CompanyConfig
        ReadOnly Property ConfigFilePath As String
        ReadOnly Property HasUnsavedChanges As Boolean

        Sub Load()
        Function Save() As Boolean
        Sub Reload()

        Sub ResetAllToDefaults()
        Sub ResetSectionToDefaults(section As String)

        Sub NotifyChange(section As String)
        Sub NotifyChangeAndSave(section As String)

        Sub EnsureAllFolders()
        Function GetSummary() As String
    End Interface
End Namespace
