using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Readme : ScriptableObject
{
    [FormerlySerializedAs("icon")]
    public Texture2D Icon;
    [FormerlySerializedAs("title")]
    public string Title;
    [FormerlySerializedAs("sections")]
    public Section[] Sections;
    [FormerlySerializedAs("loadedLayout")]
    public bool LoadedLayout;

    [Serializable]
    public class Section
    {
        [FormerlySerializedAs("heading")]
        public string Heading;
        [FormerlySerializedAs("text")]
        public string Text;
        [FormerlySerializedAs("linkText")]
        public string LinkText;
        [FormerlySerializedAs("url")]
        public string Url;
    }
}
