class Message
{
    public string text;
    public long time_s;
    public float time;
    
    public Message(string text_, float time_)
    {
        text = text_;
        time = time_;
        time_s = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
    }
    public string use()
    {
        if ((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - time_s > time*1000) return "";
        return "msg: " + text + (text[text.Length - 1] == '\n' ? "" : "\n");
    }
}
class Panel
{
    public List<Message> messages;
    string name;
    IMyTextPanel panel;
    public Panel(string name_, IMyTextPanel panel_){
        panel = panel_;
        name = name_;
        messages = new List<Message>();
    }
    public void use()
    {
        string text = "";
        foreach (Message i in messages)
        {
            string r=i.use();
            
            text += r;
        }
        panel.WriteText(text);
    }
    public bool try_add(string pname,Message msg)
    {
        if (pname == name)
        {
            messages.Add(msg);
            return true;
        }
        return false;
    }
}
void add_message(string text,string pname, float time)
{
    Message msg = new Message(text, time);
    foreach (Panel i in panels)
    {
        if (i.try_add(pname, msg)) {Echo($"add msg {text} {i.messages.Count()}");  return;}
    }
    IMyTextPanel panel = GridTerminalSystem.GetBlockWithName(pname) as IMyTextPanel;
    if (panel != null){
        Panel p=new Panel(pname, panel);
        p.try_add(pname,msg);
        panels.Add(p);
        Echo($"add panel {pname} {panels.Count()}");
}
}
List<Panel> panels;
public Program()
{
    panels = new List<Panel>();
Runtime.UpdateFrequency = UpdateFrequency.Update1;
}

void Main(string args)
{
    if (args != "")
    {
        string[] data = args.Split('|');
        add_message(data[0], data[1], float.Parse(data[2]));
    }
    foreach (Panel i in panels) i.use();
}