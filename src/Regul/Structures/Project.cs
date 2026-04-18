using System.Runtime.Serialization;
using System.Xml.Serialization;
using PleasantUI;
using ReactiveUI;

namespace Regul.Structures;

public class Project : ReactiveObject
{
    private string _idEditor = string.Empty;
    private string _path = string.Empty;
    private string _dateTime = string.Empty;

    [XmlAttribute]
    [DataMember]
    public string Path
    {
        get => _path;
        set => this.RaiseAndSetIfChanged(ref _path, value);
    }

    [XmlAttribute]
    [DataMember]
    public string IdEditor
    {
        get => _idEditor;
        set => this.RaiseAndSetIfChanged(ref _idEditor, value);
    }

    [XmlAttribute]
    [DataMember]
    public string DateTime
    {
        get => _dateTime;
        set => this.RaiseAndSetIfChanged(ref _dateTime, value);
    }

    public Project()
    {

    }

    public Project(string idEditor, string path, string dateTime)
    {
        IdEditor = idEditor;
        Path = path;
        DateTime = dateTime;
    }
}
