using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ANDREICSLIB.Helpers;
using ANDREICSLIB.Licensing;
using HappyDiff.Service_References.ServiceReference1;
using NLog;

namespace HappyDiff
{
    public partial class Form1 : Form
    {
        #region licensing

        private const string AppTitle = "HappyDiff";
        private const double AppVersion = 0.1;
        private const String HelpString = "";

        private readonly String OtherText =
            @"©" + DateTime.Now.Year +
            @" Andrei Gec (http://www.andreigec.net)
Licensed under GNU LGPL (http://www.gnu.org/)
OCR © Tessnet2/Tesseract (http://www.pixel-technology.com/freeware/tessnet2/)(https://code.google.com/p/tesseract-ocr/)
Zip Assets © SharpZipLib (http://www.sharpdevelop.net/OpenSource/SharpZipLib/)
";
        public Licensing.DownloadedSolutionDetails GetDetails()
        {
            try
            {
                var sr = new ServicesClient();
                var ti = sr.GetTitleInfo(AppTitle);
                if (ti == null)
                    return null;
                return ToDownloadedSolutionDetails(ti);

            }
            catch (Exception ex)
            {
            }
            return null;
        }

        public static Licensing.DownloadedSolutionDetails ToDownloadedSolutionDetails(TitleInfoServiceModel tism)
        {
            return new Licensing.DownloadedSolutionDetails()
            {
                ZipFileLocation = tism.LatestTitleDownloadPath,
                ChangeLog = tism.LatestTitleChangelog,
                Version = tism.LatestTitleVersion
            };
        }

        public void InitLicensing()
        {
            Licensing.CreateLicense(this, menuStrip1, new Licensing.SolutionDetails(GetDetails, HelpString, AppTitle, AppVersion, OtherText));
        }

        #endregion

        public static Logger l = LogManager.GetLogger("form");
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitLicensing();
            AsyncHelpers.RunSync(RefreshApiList);
        }

        private async Task RefreshApiList()
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
