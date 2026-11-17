# MisioEdukiTresna: Aplikatu

- **Data:** 2026-11-15 00:07:03
- **Eszena:** Assets/Resources/Misioak
- **Sektorea:** Misioen edukia (A4)

## Xehetasunak (6)

- ## Mision01_Laborategia
- - ONDO: Zientzialaria Lab1-n lanean (62.49, 0.05, 2.50)
- - ONDO: Zientzialaria Zerbitzariak-n lanean (71.25, 0.10, 43.13)
- - ONDO: Zientzialaria LabNagusia-n lanean (55.00, 0.05, 40.41)
- - ONDO: Soldadua patruilan Harrera ↔ Lab1 (25.6 m)
- - ONDO: Soldadua patruilan Borroka ↔ LabNagusia (28.0 m)

**ERROREA:** UnityEngine.MissingReferenceException: The object of type 'Eclipse.Misioak.MisioEdukia' has been destroyed but you are still trying to access it.
Your script should either check if it is null or you should not destroy the object. Parameter name: asset
  at UnityEngine.Object+MarshalledUnityObject.TryThrowEditorNullExceptionObject (UnityEngine.Object unityObj, System.String parameterName) [0x0005f] in /home/bokken/build/output/unity/unity/Runtime/Export/Scripting/UnityEngineObject.bindings.cs:1171 
  at UnityEngine.Bindings.ThrowHelper.ThrowArgumentNullException (System.Object obj, System.String parameterName) [0x0000a] in /home/bokken/build/output/unity/unity/Runtime/Export/Scripting/BindingsHelpers.cs:13 
  at UnityEditor.AssetDatabase.CreateAsset (UnityEngine.Object asset, System.String path) [0x00015] in <7d72d2d2d8944f069605e544a7caaa72>:0 
  at Eclipse.EditorTresnak.EszenaEraikitzailea.MisioEdukiExekutatu (System.Boolean aplikatu) [0x00193] in /home/ionberistain/Projects/Estudios/DAM/2AG3/Garapen teknologia aurreratuak/1_Ebaluazioa/GarapenTeknologiaAurreratuak_Unity_ECLIPSE/ECLIPSE/Assets/_Eclipse/Editor/Tresnak/MisioEdukiTresna.cs:129 
