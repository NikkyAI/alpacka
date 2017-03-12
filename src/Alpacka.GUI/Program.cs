using DevZH.UI;

namespace Alpacka.GUI
{
    public class Program : Window
    {
        public static void Main(string[] args) =>
            new Application().Run(new Program());
        
        public Program() : base("Alpacka GUI")
        {
            var tab = new Tab();
            this.Child = tab;
            
            var basicControlsPage = new BasicControlsPage("Basic Controls") { AllowMargins = true };
            tab.Children.Add(basicControlsPage);
            
            var numbersPage = new NumbersPage("Numbers and Lists") { AllowMargins = true };
            tab.Children.Add(numbersPage);
            
            var dataChoosersPage = new DataChoosersPage("Data Choosers") { AllowMargins = true };
            tab.Children.Add(dataChoosersPage);
        }
    }
}
