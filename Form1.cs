using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ANDREICSLIB.Helpers;
using ANDREICSLIB.Licensing;
using NLog;

namespace HappyDiff
{
    public partial class Form1 : Form
    {
        #region licensing
        private const String HelpString = "";

        private readonly String OtherText =
            @"©" + DateTime.Now.Year +
            @" Andrei Gec (http://www.andreigec.net)

Licensed under GNU LGPL (http://www.gnu.org/)

Zip Assets © SharpZipLib (http://www.sharpdevelop.net/OpenSource/SharpZipLib/)
";
    
        #endregion

        public static Logger l = LogManager.GetLogger("form");
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Licensing.LicensingForm(this, menuStrip1, HelpString, OtherText);
            AsyncHelpers.RunSync(RefreshApiList);
        }

        private async Task RefreshApiList()
        {
            try
            {
                var items = await Controller.GetApis();

                dataLV.Items.Clear();
                foreach (var i in items)
                {
                    var lvi = new ListViewItem(i.Name);
                    lvi.SubItems.Add(i.API1);
                    lvi.SubItems.Add(i.API2);
                    dataLV.Items.Add(lvi);
                }
                ANDREICSLIB.ClassExtras.ListViewExtras.AutoResize(dataLV);
            }
            catch (Exception ex)
            {

                throw;
            }

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void addNewTestToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            var res = ANDREICSLIB.NewControls.MassVariableEdit.ShowDialogStatic("Enter new connection", new APICall());
            if (res == null)
                return;

            var items = AsyncHelpers.RunSync(Controller.GetApis);

            items.Add(res);
            AsyncHelpers.RunSync(() => Controller.SetApis(items));
            AsyncHelpers.RunSync(RefreshApiList);
        }

        private void processToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AsyncHelpers.RunSync(Controller.Run);
        }
    }

    public class APICall
    {
        public string Name { get; set; }
        public string API1 { get; set; }
        public string API2 { get; set; }
    }
}
