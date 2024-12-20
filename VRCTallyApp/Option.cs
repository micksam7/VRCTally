using Terminal.Gui;

public class Option : View
{
    public TextField tf;
    public Label lbl;

    public Option(string title, string value)
    {
        //default height 1
        Width = Dim.Fill();
        Height = 1;

        var lbl = new Label(title) { X = 0, Y = 0 };
        Add(lbl);
        tf = new TextField("")
        {
            X = Pos.Right(lbl),
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            Text = value,
        };
        Add(tf);
    }
}
