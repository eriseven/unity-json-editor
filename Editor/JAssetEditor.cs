using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine;
using UnityEditor;
using System;
using GameStudio.Engine.Workflow;


// Adds a nice editor to edit JSON files as well as a simple text editor incase
// the editor doesn't support the types you need. It works with strings, floats
// ints and bools at the moment.
// 
// * Requires the latest version of JSON.net compatible with Unity


//If you want to edit a JSON file in the "StreammingAssets" Folder change this to DefaultAsset.
//Hacky solution to a weird problem :/
[CustomEditor(typeof(JsonAsset), true)]
public class JAssetEditor : Editor
{
    
    private string Path => AssetDatabase.GetAssetPath(target);
    private bool isCompatible => Path.EndsWith(".asset");
    private bool unableToParse => !NewtonsoftExtensions.IsValidJson(rawText);

    private bool isTextMode, wasTextMode;

    private string rawText;

    private JsonAsset jAssetIns;
    private JObject jsonObject;
    private JProperty propertyToRename;
    private string propertyRename;


    private void OnEnable()
    {
        if (isCompatible)
            LoadFromJAsset();
    }

    private void OnDisable()
    {
        if (isCompatible)
            WriteToJAsset();
    }

    public override void OnInspectorGUI()
    {
        if (isCompatible)
        {
            JsonInspectorGUI();
            return;
        }
        base.OnInspectorGUI();
    }

    private void JsonInspectorGUI()
    {
        bool enabled = GUI.enabled;
        GUI.enabled = true;

        Rect subHeaderRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 2.5f);
        Rect helpBoxRect = new Rect(subHeaderRect.x, subHeaderRect.y, subHeaderRect.width - subHeaderRect.width / 6 - 5f, subHeaderRect.height);
        Rect rawTextModeButtonRect = new Rect(subHeaderRect.x + subHeaderRect.width / 6 * 5, subHeaderRect.y, subHeaderRect.width / 6, subHeaderRect.height);

        EditorGUI.HelpBox(helpBoxRect, "You edit raw text if the JSON editor isn't enough by clicking the button to the right", MessageType.Info);

        GUIStyle wrappedButton = new GUIStyle("Button");
        wrappedButton.wordWrap = true;
        EditorGUI.BeginChangeCheck();
        GUI.enabled = !unableToParse;
        isTextMode = GUI.Toggle(rawTextModeButtonRect, isTextMode, "Edit Text", wrappedButton);
        if(EditorGUI.EndChangeCheck())
        {
            WriteToJAsset();
            GUI.FocusControl("");
            LoadFromJAsset();
        }
        GUI.enabled = true;

        GUILayout.BeginVertical();
        if (!isTextMode)
        {
            if (jsonObject != null)
            {
                RecursiveDrawField(jsonObject);
                if(GUILayout.Button("Add New Property"))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Empty Object"), false, () =>
                    {
                        AddNewProperty<JObject>(jsonObject);
                    });
                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("String"), false, () =>
                    {
                        AddNewProperty<string>(jsonObject);
                    });
                    menu.AddItem(new GUIContent("Single"), false, () =>
                    {
                        AddNewProperty<float>(jsonObject);
                    });
                    menu.AddItem(new GUIContent("Integer"), false, () =>
                    {
                        AddNewProperty<int>(jsonObject);
                    });
                    menu.AddItem(new GUIContent("Boolean"), false, () =>
                    {
                        AddNewProperty<bool>(jsonObject);

                    });
                    menu.AddItem(new GUIContent("List"), false, () =>
                    {
                        AddNewProperty<JArray>(jsonObject);

                    });
                    menu.AddItem(new GUIContent("Add JAsset"), false, () =>
                    {
                        jAssetIns.jAssetList.Add(null);
                    });
                    for (int k = 0; k < jAssetIns.jAssetList.Count(); ++k)
                    {
                        if (jAssetIns.jAssetList[k] != null)
                        {
                            var tmpAssetRawtext = jAssetIns.jAssetList[k].rawtext;
                            var name = jAssetIns.jAssetList[k].name;
                            var tmpJObject = JsonConvert.DeserializeObject<JObject>(tmpAssetRawtext);
                            menu.AddItem(new GUIContent("Add/" + name), false, () =>
                            {
                                AddJAssetObject(tmpJObject, jsonObject);
                            });
                        }
                    }
                    menu.ShowAsContext();
                }
                GUILayout.Label("Asset List");
                JsonAsset insSelf = null;
                for(int i = 0; i< jAssetIns.jAssetList.Count(); ++i)
                {
                    var obj = jAssetIns.jAssetList[i];
                    obj = EditorGUILayout.ObjectField("", obj, typeof(JsonAsset), false) as JsonAsset;
                    if(obj == target)
                    {
                        insSelf = obj;
                    }
                    jAssetIns.jAssetList[i] = obj;
                }
                if(insSelf)
                {
                    jAssetIns.jAssetList.Remove(insSelf);
                }
            }
        }
        else
        {
            rawText = EditorGUILayout.TextArea(rawText);
            GUIStyle helpBoxRichText = new GUIStyle(EditorStyles.helpBox);
            Texture errorIcon = EditorGUIUtility.Load("icons/console.erroricon.png") as Texture2D;

            helpBoxRichText.richText = true;

            if (unableToParse)
                GUILayout.Label(new GUIContent("Unable to parse text into JSON. Make sure there are no mistakes! Are you missing a <b>{</b>, <b>{</b> or <b>,</b>?", errorIcon), helpBoxRichText);
        }

        wasTextMode = isTextMode;
        GUI.enabled = enabled;
        GUILayout.EndVertical();

    }
    //original
    //private void _JsonInspectorGUI()
    //{
    //    bool enabled = GUI.enabled;
    //    GUI.enabled = true;

    //    Rect subHeaderRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * 2.5f);
    //    Rect helpBoxRect = new Rect(subHeaderRect.x, subHeaderRect.y, subHeaderRect.width - subHeaderRect.width / 6 - 5f, subHeaderRect.height);
    //    Rect rawTextModeButtonRect = new Rect(subHeaderRect.x + subHeaderRect.width / 6 * 5, subHeaderRect.y, subHeaderRect.width / 6, subHeaderRect.height);
    //    EditorGUI.HelpBox(helpBoxRect, "You edit raw text if the JSON editor isn't enough by clicking the button to the right", MessageType.Info);
        

    //    GUIStyle wrappedButton = new GUIStyle("Button");
    //    wrappedButton.wordWrap = true;
    //    EditorGUI.BeginChangeCheck();
    //    GUI.enabled = !unableToParse;
    //    isTextMode = GUI.Toggle(rawTextModeButtonRect, isTextMode, "Edit Text", wrappedButton);
    //    if(EditorGUI.EndChangeCheck())
    //    {
    //        WriteToJson();
    //        GUI.FocusControl("");
    //        LoadFromJson();
    //    }
    //    GUI.enabled = true;

    //    if (!isTextMode)
    //    {
    //        if (jsonObject != null)
    //        {
    //            Rect initialRect = new Rect(10, 5 + EditorGUIUtility.singleLineHeight * 6, EditorGUIUtility.currentViewWidth - 20, EditorGUIUtility.singleLineHeight);

    //            int fieldsHeight = RecursiveDrawField(new Rect(initialRect.x + 5, initialRect.y, initialRect.width, initialRect.height), jsonObject);
    //            fieldsHeight += 3;
    //            Rect addNewButton = new Rect(initialRect.x, initialRect.y + fieldsHeight * initialRect.height, initialRect.width, initialRect.height * 2);
    //            if(GUI.Button(addNewButton, "Add New Property"))
    //            {
    //                GenericMenu menu = new GenericMenu();
    //                menu.AddItem(new GUIContent("Empty Object"), false, () =>
    //                {
    //                    AddNewProperty<JObject>(jsonObject);
    //                });
    //                menu.AddSeparator("");
    //                menu.AddItem(new GUIContent("String"), false, () =>
    //                {
    //                    AddNewProperty<string>(jsonObject);
    //                });
    //                menu.AddItem(new GUIContent("Single"), false, () =>
    //                {
    //                    AddNewProperty<float>(jsonObject);
    //                });
    //                menu.AddItem(new GUIContent("Integer"), false, () =>
    //                {
    //                    AddNewProperty<int>(jsonObject);
    //                });
    //                menu.AddItem(new GUIContent("Boolean"), false, () =>
    //                {
    //                    AddNewProperty<bool>(jsonObject);

    //                });
    //                menu.DropDown(addNewButton);
    //            }
    //        }
    //    }
    //    else
    //    {
    //        float textFieldHeight = GUI.skin.label.CalcSize(new GUIContent(rawText)).y;
    //        Rect textFieldRect = new Rect(subHeaderRect.x, subHeaderRect.y + subHeaderRect.height + EditorGUIUtility.singleLineHeight, subHeaderRect.width, textFieldHeight);
    //        rawText = EditorGUI.TextArea(textFieldRect, rawText);
    //        Rect errorBoxRect = new Rect(textFieldRect.x, textFieldRect.y + textFieldRect.height + EditorGUIUtility.singleLineHeight, textFieldRect.width, EditorGUIUtility.singleLineHeight * 2.5f);
    //        GUIStyle helpBoxRichText = new GUIStyle(EditorStyles.helpBox);
    //        Texture errorIcon = EditorGUIUtility.Load("icons/console.erroricon.png") as Texture2D;

    //        helpBoxRichText.richText = true;

    //        if (unableToParse)
    //            GUI.Label(errorBoxRect, new GUIContent("Unable to parse text into JSON. Make sure there are no mistakes! Are you missing a <b>{</b>, <b>{</b> or <b>,</b>?", errorIcon), helpBoxRichText);
    //    }

    //    wasTextMode = isTextMode;
    //    GUI.enabled = enabled;
    //}
    private void ShowArray(ref JToken jT)
    {
        JTokenType jType = jT.Type;
        switch(jType)
        {
            case JTokenType.String:
                string stringValue = jT.Value<string>();
                stringValue = EditorGUILayout.TextField(stringValue);
                jT = stringValue;
                break;
            case JTokenType.Float:
                float floatValue = jT.Value<float>();
                floatValue = EditorGUILayout.FloatField(floatValue);
                jT = floatValue;
                break;
            case JTokenType.Integer:
                int intValue = jT.Value<int>();
                intValue = EditorGUILayout.IntField(intValue);
                jT = intValue;
                break;
            case JTokenType.Boolean:
                bool boolValue = jT.Value<bool>();
                boolValue = EditorGUILayout.Toggle(boolValue);
                jT = boolValue;
                break;
            case JTokenType.Object:
                RecursiveDrawField(jT);
                break;
            case JTokenType.Array:
                RecursiveDrawField(jT);
                break;
            case JTokenType.Null:
                float textFieldWidth = EditorStyles.helpBox.CalcSize(new GUIContent("Null")).x;
                //GUI.Label(new Rect(rect.x, rect.y, textFieldWidth, rect.height), "Null", EditorStyles.helpBox);
                break;
            default:
                //GUI.Label(new Rect(rect.x, rect.y, rect.width, rect.height), string.Format("Type '{0}' is not supported. Use text editor instead", token.Type.ToString()), EditorStyles.helpBox);
                break;
        }
    }

    private int RecursiveDrawField(JToken container)
    {
        int j = 0;
        for (int i = 0; i < container.Count(); i++)
        {
            JToken token = container.Values<JToken>().ToArray()[i];
            GUILayout.BeginHorizontal();
            if (token.Type == JTokenType.Property)
            {
                JProperty property = token.Value<JProperty>();

                string propertyName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(property.Name.ToLower()) + ":";
                float propertyNameWidth = GUI.skin.label.CalcSize(new GUIContent(propertyName)).x;
                //Rect propertyNameRect = new Rect(rect.x + rect.height, rect.y + EditorGUIUtility.singleLineHeight * j * 1.3f, propertyNameWidth, rect.height);

                //Rect buttonRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * j * 1.3f, rect.height, rect.height);
                GUIStyle buttonContent = new GUIStyle(EditorStyles.label);
                buttonContent.normal.textColor = Color.grey;

                if (propertyToRename != property)
                {
                    if (GUILayout.Button("►"))
                    {
                        GenericMenu menu = new GenericMenu();
                        if (property.Value.Type == JTokenType.Object )
                        {
                            JObject jObject = property.Value.Value<JObject>();
                            menu.AddItem(new GUIContent("Add/Empty Object"), false, () =>
                            {
                                AddNewProperty<JObject>(jObject);
                            });
                            menu.AddSeparator("Add/");
                            menu.AddItem(new GUIContent("Add/String"), false, () =>
                            {
                                AddNewProperty<string>(jObject);
                            });
                            menu.AddItem(new GUIContent("Add/Single"), false, () =>
                            {
                                AddNewProperty<float>(jObject);
                            });
                            menu.AddItem(new GUIContent("Add/Integer"), false, () =>
                            {
                                AddNewProperty<int>(jObject);
                            });
                            menu.AddItem(new GUIContent("Add/Boolean"), false, () =>
                            {
                                AddNewProperty<bool>(jObject);

                            });
                            menu.AddItem(new GUIContent("Add/List"), false, () =>
                            {
                                AddNewProperty<JArray>(jObject);
                            });
                            for(int k = 0; k < jAssetIns.jAssetList.Count(); ++k)
                            {
                                if(jAssetIns.jAssetList[k] != null)
                                {
                                    var tmpAssetRawtext = jAssetIns.jAssetList[k].rawtext;
                                    var name = jAssetIns.jAssetList[k].name;
                                    var tmpJObject = JsonConvert.DeserializeObject<JObject>(tmpAssetRawtext);
                                    menu.AddItem(new GUIContent("Add/" + name), false, () =>
                                    {
                                        AddJAssetObject(tmpJObject, jObject);
                                    });
                                }
                            }
                        }
                        else if(property.Value.Type == JTokenType.Array)
                        {
                            JArray jArray = property.Value.Value<JArray>();
                            menu.AddItem(new GUIContent("Add/Empty Object"), false, () =>
                            {
                                AddNewValueToArray<JObject>(jArray);
                            });
                            menu.AddSeparator("Add/");
                            menu.AddItem(new GUIContent("Add/String"), false, () =>
                            {
                                AddNewValueToArray<string>(jArray);
                            });
                            menu.AddItem(new GUIContent("Add/Single"), false, () =>
                            {
                                AddNewValueToArray<float>(jArray);
                            });
                            menu.AddItem(new GUIContent("Add/Integer"), false, () =>
                            {
                                AddNewValueToArray<int>(jArray);
                            });
                            menu.AddItem(new GUIContent("Add/Boolean"), false, () =>
                            {
                                AddNewValueToArray<bool>(jArray);
                            });
                            menu.AddItem(new GUIContent("Add/List"), false, () =>
                            {
                                AddNewValueToArray<JArray>(jArray);
                            });
                            for (int k = 0; k < jAssetIns.jAssetList.Count(); ++k)
                            {
                                if(jAssetIns.jAssetList[k] != null)
                                {
                                    var tmpAssetRawtext = jAssetIns.jAssetList[k].rawtext;
                                    var name = jAssetIns.jAssetList[k].name;
                                    var tmpJObject = JsonConvert.DeserializeObject<JObject>(tmpAssetRawtext);
                                    menu.AddItem(new GUIContent("Add/" + name), false, () =>
                                    {
                                        AddNewObjectToArray(tmpJObject, jArray);
                                    });
                                }
                            }
                        }
                        menu.AddItem(new GUIContent("Remove"), false, () => {
                            token.Remove();
                        });
                        menu.AddSeparator("");
                        menu.AddItem(new GUIContent("Rename"), false, () => {
                            propertyToRename = property;
                            propertyRename = propertyToRename.Name;
                        });
                        menu.ShowAsContext();
                    }
                    GUILayout.Label(propertyName);
                }
                else
                {
                    //Rect propertyTextFieldRect = new Rect(propertyNameRect.x + 2, propertyNameRect.y, propertyNameRect.width - 4, propertyNameRect.height);
                    GUI.SetNextControlName("RENAME_PROPERTY");
                    propertyRename = GUILayout.TextField(propertyRename);

                    GUI.color = new Color32(109, 135, 111, 255);
                    GUI.enabled = !string.IsNullOrEmpty(propertyRename);
                    if (GUILayout.Button("✔"))
                    {
                        property.Value.Rename(propertyRename);
                        GUI.FocusControl("");
                    }
                    GUI.color = Color.white;
                    buttonContent.normal.textColor = new Color32(133, 229, 143, 255);
                    GUI.enabled = true;
                }
                //Rect nextRect = new Rect(rect.x + rect.height + propertyNameWidth, rect.y + EditorGUIUtility.singleLineHeight * j * 1.3f, rect.width - propertyNameWidth - rect.height, rect.height);
                j += RecursiveDrawField(token);
            }
            else if (token.Type == JTokenType.Object) 
            {
                GUILayout.BeginVertical();
                j += RecursiveDrawField(token);
                GUILayout.EndVertical();
            }
            else if (token.Type == JTokenType.Array)
            {
                RecursiveDrawArray(token);
            }
            else
            {
                JProperty parentProperty = token.Parent.Value<JProperty>();
                //33
                switch (token.Type)
                {
                    case JTokenType.String:
                        string stringValue = token.Value<string>();
                        stringValue = EditorGUILayout.TextField(stringValue);
                        parentProperty.Value = stringValue;
                        break;
                    case JTokenType.Float:
                        float floatValue = token.Value<float>();
                        floatValue = EditorGUILayout.FloatField(floatValue);
                        parentProperty.Value = floatValue;
                        break;
                    case JTokenType.Integer:
                        int intValue = token.Value<int>();
                        intValue = EditorGUILayout.IntField(intValue);
                        parentProperty.Value = intValue;
                        break;
                    case JTokenType.Boolean:
                        bool boolValue = token.Value<bool>();
                        boolValue = EditorGUILayout.Toggle(boolValue);
                        parentProperty.Value = boolValue;
                        break;
                    case JTokenType.Null:
                        float textFieldWidth = EditorStyles.helpBox.CalcSize(new GUIContent("Null")).x;
                        //GUI.Label(new Rect(rect.x, rect.y, textFieldWidth, rect.height), "Null", EditorStyles.helpBox);
                        break;
                    default:
                        //GUI.Label(new Rect(rect.x, rect.y, rect.width, rect.height), string.Format("Type '{0}' is not supported. Use text editor instead", token.Type.ToString()), EditorStyles.helpBox);
                        break;
                }
                j++;
            }
            GUILayout.EndHorizontal();
        }
        return Mathf.Max(j, 1);
    }
    private void RecursiveDrawArray(JToken token)
    {
        var jArray = token.Value<JArray>();
        var _delList = new List<JToken>();
        GUILayout.BeginVertical();
        for (int k = 0; k < jArray.Count(); ++k)
        {
            JTokenType jType = jArray[k].Type;
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-"))
            {
                _delList.Add(jArray[k]);
            }
            switch (jType)
            {
                case JTokenType.String:
                    GUILayout.Label("str_" + (k + 1));
                    string stringValue = jArray[k].Value<string>();
                    stringValue = EditorGUILayout.TextField(stringValue);
                    jArray[k] = stringValue;
                    break;
                case JTokenType.Float:
                    GUILayout.Label("float_" + (k + 1));
                    float floatValue = jArray[k].Value<float>();
                    floatValue = EditorGUILayout.FloatField(floatValue);
                    jArray[k] = floatValue;
                    break;
                case JTokenType.Integer:
                    GUILayout.Label("int_" + (k + 1));
                    int intValue = jArray[k].Value<int>();
                    intValue = EditorGUILayout.IntField(intValue);
                    jArray[k] = intValue;
                    break;
                case JTokenType.Boolean:
                    GUILayout.Label("bool_" + (k + 1));
                    bool boolValue = jArray[k].Value<bool>();
                    boolValue = EditorGUILayout.Toggle(boolValue);
                    jArray[k] = boolValue;
                    break;
                case JTokenType.Object:
                    if (GUILayout.Button("►"))
                    {
                        GenericMenu menu = new GenericMenu();
                        JObject jObject = (JObject)jArray[k];
                        menu.AddItem(new GUIContent("Add/String"), false, () =>
                        {
                            AddNewProperty<string>(jObject);
                        });
                        menu.AddItem(new GUIContent("Add/Single"), false, () =>
                        {
                            AddNewProperty<float>(jObject);
                        });
                        menu.AddItem(new GUIContent("Add/Integer"), false, () =>
                        {
                            AddNewProperty<int>(jObject);
                        });
                        menu.AddItem(new GUIContent("Add/Boolean"), false, () =>
                        {
                            AddNewProperty<bool>(jObject);

                        });
                        menu.AddItem(new GUIContent("Add/List"), false, () =>
                        {
                            AddNewProperty<JArray>(jObject);
                        });
                        menu.AddItem(new GUIContent("Add/Empty Object"), false, () =>
                        {
                            AddNewProperty<JObject>(jObject);
                        });
                        menu.ShowAsContext();
                    }
                    GUILayout.Label("obj_" + (k + 1));
                    GUILayout.BeginVertical();
                    RecursiveDrawField(jArray[k]);
                    GUILayout.EndVertical();
                    break;
                case JTokenType.Array:
                    if (GUILayout.Button("►"))
                    {
                        GenericMenu menu = new GenericMenu();
                        JArray ja = (JArray)jArray[k];
                        menu.AddItem(new GUIContent("Add/String"), false, () =>
                        {
                            AddNewValueToArray<string>(ja);
                        });
                        menu.AddItem(new GUIContent("Add/Single"), false, () =>
                        {
                            AddNewValueToArray<float>(ja);
                        });
                        menu.AddItem(new GUIContent("Add/Integer"), false, () =>
                        {
                            AddNewValueToArray<int>(ja);
                        });
                        menu.AddItem(new GUIContent("Add/Boolean"), false, () =>
                        {
                            AddNewValueToArray<bool>(ja);

                        });
                        menu.AddItem(new GUIContent("Add/List"), false, () =>
                        {
                            AddNewValueToArray<JArray>(ja);
                        });
                        menu.AddItem(new GUIContent("Add/Empty Object"), false, () =>
                        {
                            AddNewValueToArray<JObject>(ja);
                        });
                        menu.ShowAsContext();
                    }
                    GUILayout.Label("list_" + (k + 1));
                    RecursiveDrawArray(jArray[k]);
                    break;
                case JTokenType.Null:
                    Debug.LogError("JTokenType is Null " + jType);
                    //float textFieldWidth = EditorStyles.helpBox.CalcSize(new GUIContent("Null")).x;
                    //GUI.Label(new Rect(rect.x, rect.y, textFieldWidth, rect.height), "Null", EditorStyles.helpBox);
                    break;
                default:
                    Debug.LogError("Unknow JTokenType: " + jType + "use text editor instead!");
                    //GUI.Label(new Rect(rect.x, rect.y, rect.width, rect.height), string.Format("Type '{0}' is not supported. Use text editor instead", token.Type.ToString()), EditorStyles.helpBox);
                    break;
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        foreach (var item in _delList)
        {
            jArray.Remove(item);
        }
    }
    //original
    //private int _RecursiveDrawField(Rect rect, JToken container)
    //{
    //    int j = 0;
    //    for (int i = 0; i < container.Count(); i++)
    //    {
    //        JToken token = container.Values<JToken>().ToArray()[i];

    //        if (token.Type == JTokenType.Property)
    //        {
    //            JProperty property = token.Value<JProperty>();

    //            string propertyName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(property.Name.ToLower()) + ":";
    //            float propertyNameWidth = GUI.skin.label.CalcSize(new GUIContent(propertyName)).x;
    //            Rect propertyNameRect = new Rect(rect.x + rect.height, rect.y + EditorGUIUtility.singleLineHeight * j * 1.3f, propertyNameWidth, rect.height);

    //            Rect buttonRect = new Rect(rect.x, rect.y + EditorGUIUtility.singleLineHeight * j * 1.3f, rect.height, rect.height);
    //            GUIStyle buttonContent = new GUIStyle(EditorStyles.label);
    //            buttonContent.normal.textColor = Color.grey;

    //            if (propertyToRename != property)
    //            {
    //                if (GUI.Button(buttonRect, GUIContent.none, EditorStyles.miniButton))
    //                {
    //                    GenericMenu menu = new GenericMenu();
    //                    if (property.Value.Type == JTokenType.Object)
    //                    {
    //                        JObject jObject = property.Value.Value<JObject>();
    //                        menu.AddItem(new GUIContent("Add/Empty Object"), false, () =>
    //                        {
    //                            AddNewProperty<JObject>(jObject);
    //                        });
    //                        menu.AddSeparator("Add/");
    //                        menu.AddItem(new GUIContent("Add/String"), false, () =>
    //                        {
    //                            AddNewProperty<string>(jObject);
    //                        });
    //                        menu.AddItem(new GUIContent("Add/Single"), false, () =>
    //                        {
    //                            AddNewProperty<float>(jObject);
    //                        });
    //                        menu.AddItem(new GUIContent("Add/Integer"), false, () =>
    //                        {
    //                            AddNewProperty<int>(jObject);
    //                        });
    //                        menu.AddItem(new GUIContent("Add/Boolean"), false, () =>
    //                        {
    //                            AddNewProperty<bool>(jObject);

    //                        });
    //                    }
    //                    menu.AddItem(new GUIContent("Remove"), false, () => {
    //                        token.Remove();
    //                    });
    //                    menu.AddSeparator("");
    //                    menu.AddItem(new GUIContent("Rename"), false, () => {
    //                        propertyToRename = property;
    //                        propertyRename = propertyToRename.Name;
    //                    });
    //                    menu.DropDown(buttonRect);
    //                }
    //                GUI.Label(propertyNameRect, propertyName);
    //                GUI.Label(buttonRect, "►", buttonContent);
    //            }
    //            else
    //            {
    //                Rect propertyTextFieldRect = new Rect(propertyNameRect.x + 2, propertyNameRect.y, propertyNameRect.width - 4, propertyNameRect.height);
    //                GUI.SetNextControlName("RENAME_PROPERTY");
    //                propertyRename = EditorGUI.TextField(propertyTextFieldRect, propertyRename);

    //                GUI.color = new Color32(109, 135, 111, 255);
    //                GUI.enabled = !string.IsNullOrEmpty(propertyRename);
    //                if (GUI.Button(buttonRect, GUIContent.none, EditorStyles.miniButton))
    //                {
    //                    property.Value.Rename(propertyRename);
    //                    GUI.FocusControl("");
    //                }
    //                GUI.color = Color.white;
    //                buttonContent.normal.textColor = new Color32(133, 229, 143, 255);
    //                GUI.Label(buttonRect, "✔", buttonContent);
    //                GUI.enabled = true;
    //            }
    //            Rect nextRect = new Rect(rect.x + rect.height + propertyNameWidth, rect.y + EditorGUIUtility.singleLineHeight * j * 1.3f, rect.width - propertyNameWidth - rect.height, rect.height);
    //            j += RecursiveDrawField(nextRect, token);
    //        }
    //        else if (token.Type == JTokenType.Object)
    //        {
    //            j += RecursiveDrawField(rect, token);
    //        }
    //        else
    //        {
    //            JProperty parentProperty = token.Parent.Value<JProperty>();
                
    //            switch (token.Type)
    //            {
    //                case JTokenType.String:
    //                    string stringValue = token.Value<string>();
    //                    stringValue = EditorGUI.TextField(rect, stringValue);
    //                    parentProperty.Value = stringValue;
    //                    break;
    //                case JTokenType.Float:
    //                    float floatValue = token.Value<float>();
    //                    floatValue = EditorGUI.FloatField(rect, floatValue);
    //                    parentProperty.Value = floatValue;
    //                    break;
    //                case JTokenType.Integer:
    //                    int intValue = token.Value<int>();
    //                    intValue = EditorGUI.IntField(rect, intValue);
    //                    parentProperty.Value = intValue;
    //                    break;
    //                case JTokenType.Boolean:
    //                    bool boolValue = token.Value<bool>();
    //                    boolValue = EditorGUI.Toggle(rect, boolValue);
    //                    parentProperty.Value = boolValue;
    //                    break;
    //                case JTokenType.Null:
    //                    float textFieldWidth = EditorStyles.helpBox.CalcSize(new GUIContent("Null")).x;
    //                    GUI.Label(new Rect(rect.x, rect.y, textFieldWidth, rect.height), "Null", EditorStyles.helpBox);
    //                    break;
    //                default:
    //                    GUI.Label(new Rect(rect.x, rect.y, rect.width, rect.height), string.Format("Type '{0}' is not supported. Use text editor instead", token.Type.ToString()), EditorStyles.helpBox);
    //                    break;
    //            }
    //            j++;
    //        }
    //    }
    //    return Mathf.Max(j, 1);
    //}

    private void LoadFromJAsset()
    {
        if (!string.IsNullOrWhiteSpace(Path))
        {
            jAssetIns = AssetDatabase.LoadAssetAtPath<JsonAsset>(Path);
            rawText = jAssetIns.rawtext;
            jsonObject = JsonConvert.DeserializeObject<JObject>(rawText);
        }

    }

    private void WriteToJAsset()
    {
        if (jsonObject != null)
        {
            if (!wasTextMode)
                rawText = jsonObject.ToString();
            jAssetIns.rawtext = rawText;
            UnityEditor.EditorUtility.SetDirty(jAssetIns);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
        }
    }
    private void AddNewValueToArray<T>(JArray jArray)
    {
        string typeName = typeof(T).Name.ToLower();
        object value = default(T);
        switch (Type.GetTypeCode(typeof(T)))
        {
            case TypeCode.Boolean:
                break;
            case TypeCode.Int32:
                typeName = "integer";
                break;
            case TypeCode.Single:
                break;
            case TypeCode.String:
                value = "";
                break;
            default:
                if (typeof(T) == typeof(JObject))
                    typeName = "empty object";
                value = new JObject();
                break;
        }
        if (typeof(T) == typeof(JArray))
        {
            value = new JArray();
        }
        //var jValue = new JValue(value);
        jArray.Add(value);
    }
    private void AddNewProperty<T>(JObject jObject)
    {
        string typeName = typeof(T).Name.ToLower();
        object value = default(T);
        switch (Type.GetTypeCode(typeof(T)))
        {
            case TypeCode.Boolean:
                break;
            case TypeCode.Int32:
                typeName = "integer";
                break;
            case TypeCode.Single:
                break;
            case TypeCode.String:
                value = "";
                break;
            default:
                if(typeof(T) == typeof(JObject))
                    typeName = "empty object";
                    value = new JObject();
                break;
        }
        if(typeof(T) == typeof(JArray))
        {
            value = new JArray();
        }
        string name = GetUniqueName(jObject, string.Format("new {0}", typeName));
        JProperty property = new JProperty(name, value);
        jObject.Add(property);
    }
    private void AddJAssetObject(JObject assetObj, JObject jObject)
    {
        var value = assetObj;
        string name = GetUniqueName(jObject, string.Format("new asset object"));
        JProperty property = new JProperty(name, value);
        jObject.Add(property);
    }
    private void AddNewObjectToArray(JObject jo, JArray jArray)
    {
        jArray.Add(jo);
    }
    private void AddImportedJsonObject(JObject jo, JObject originalJObject)
    {
        JProperty property = new JProperty(name, jo);
        originalJObject.Add(property);
    }
    private string GetUniqueName(JObject jObject, string orignalName)
    {
        string uniqueName = orignalName;
        int suffix = 0;
        while (jObject[uniqueName] != null && suffix < 100)
        {
            suffix++;
            if (suffix >= 100)
            {
                Debug.LogError("Stop calling all your fields the same thing! Isn't it confusing?");
            }
            uniqueName = string.Format("{0} {1}", orignalName, suffix.ToString());
        }
        return uniqueName;
    }

   
    [MenuItem("Assets/Create/JAsset File", priority = 82)]
    public static void CreateNewJAssetFile()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (path == "")
            path = "Assets";
        else if (System.IO.Path.GetExtension(path) != "")
            path = path.Replace(System.IO.Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
        string filename = "/New JAsset File";
        string tail = ".asset";
        string filepath = path + filename + tail;
        Debug.Log("filepath " + filepath);
        int i = 0;
        while(File.Exists(filepath))
        {
            filename = filename + i;
            filepath = path + filename + tail;
            Debug.Log("filename " + filename);
        }
        filename = filename.Substring(1)+ tail;
        Debug.Log("filename " + filename);
        path = System.IO.Path.Combine(path, filename);

        //JsonAsset jAsset = new JsonAsset();
        var jAsset = CreateInstance<JsonAsset>();
        AssetDatabase.CreateAsset(jAsset, path);
        AssetDatabase.Refresh();
    }
}
