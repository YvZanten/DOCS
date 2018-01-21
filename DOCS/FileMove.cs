using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Drawing;

namespace DOCS
{
    class FileMove
    {
        /// <summary>
        /// Path to source file
        /// </summary>
        public string PathSrc;

        /// <summary>
        /// Path to destination file, includes filename
        /// </summary>
        public string PathDst;

        /// <summary>
        /// Type of file
        /// </summary>
        public StateFileType Type;

        /// <summary>
        /// If file should be moved, defaults to true
        /// </summary>
        public bool move;

        /// <summary>
        /// If file should be in log, defaults to true
        /// </summary>
        public bool log;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Source">Path to source file</param>
        /// <param name="Destination">Path to destination file, needs filename included</param>
        /// <param name="FileType">Type of file</param>
        public FileMove(string Source = "", string Destination = "", StateFileType FileType = StateFileType.UNKNOWN)
        {
            PathSrc = Source;
            PathDst = Destination;
            Type = FileType;
            move = true;
        }
        
        /// <summary>
        /// Attempt to move file
        /// </summary>
        /// <returns>If no errors occur, return empty string. Else, return string with error.</returns>
        public string MoveFile()
        {
            bool error = false;
            string errorMsg = "Errors have occured while trying to move a file:\r\n\t" + PathSrc + "\r\n\r\n";

            string GenError = GeneratePathFolders(PathDst);
            if(GenError != "")
            {
                errorMsg += "ERROR: Could not generate path to destination!\r\n\r\n" + GenError;
                error = true;
            }

            if (!File.Exists(PathSrc))
            {
                error = true;
                errorMsg += "ERROR: Could not find given source file!\r\n\r\n";
            }
            if (File.Exists(PathDst))
            {
                error = true;
                errorMsg += "ERROR: Destination path (file) already exists!\r\n\t" + PathDst + "\r\n\r\n";
            }

            try
            {
                if (!error)
                    File.Move(PathSrc, PathDst);
            }
            catch (Exception e)
            {
                error = true;
                errorMsg += "ERROR: " + e.GetType().ToString() + "\r\n" + e.Message + "\r\n\tSRC: " + PathSrc + "\r\n\tDST: " + PathDst;
            }

            if (error)
            {
                return errorMsg;
            }

            return "";
        }

        /// <summary>
        /// Creates folders for given path (if needed)
        /// </summary>
        /// <param name="filePath">Path to generate</param>
        /// <returns>Returns Created path</returns>
        private string GeneratePathFolders(string filePath)
        {
            string[] pathSplit = filePath.Split('\\');
            string path = pathSplit[0];
            string TDLCaTMeetingFolder = "";

            //for each part of the path
            for (int index = 1; index < pathSplit.Length - 1; index++)
            {
                //if path part is "xxth TDL CaT - "
                if (pathSplit[index].Contains(" TDL CaT - "))
                {
                    string[] dirs = Directory.GetDirectories(path + '\\');
                    bool MeetingFolderExists = false;

                    //check if directory for meeting already exists
                    foreach (string dir in dirs)
                    {
                        TDLCaTMeetingFolder = dir.Split('\\').Last<string>();

                        if (dir.Split('\\').Last<string>().Contains(pathSplit[index]))
                        {
                            MeetingFolderExists = true;
                            break;
                        }
                    }

                    //if not, prompt user and create new folder
                    if (!MeetingFolderExists)
                    {
                        string prompt = Prompt_TDLCaTFolderDate(pathSplit[index]);
                        if (prompt == "")
                            return "";

                        path += '\\' + pathSplit[index] + prompt;
                        try
                        {
                            Directory.CreateDirectory(path);
                        }
                        catch
                        {
                            return "";
                        }

                        continue;
                    }
                    //else, append existing folder to path
                    else
                        path += '\\' + TDLCaTMeetingFolder;
                }
                //else, continue as usual
                else
                    path += '\\' + pathSplit[index];

                //try and create folder
                try
                {
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                }
                catch
                {
                    return "";
                }
            }

            return path;
        }

        //prompts user for TDL CAT folder date
        private string Prompt_TDLCaTFolderDate(string path)
        {
            Form prompt = new Form()
            {
                Width = 400 + SystemInformation.BorderSize.Width * 2,
                Height = 60 + SystemInformation.BorderSize.Height * 2,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                Text = "Enter path date description",
                StartPosition = FormStartPosition.CenterScreen,
                MinimumSize = new Size(300, 60 + SystemInformation.BorderSize.Height * 2),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            TableLayoutPanel tbl_Base = new TableLayoutPanel()
            {
                ColumnCount = 3,
                RowCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(0),
                Margin = new Padding(0),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };
            tbl_Base.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize, 0));
            tbl_Base.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            tbl_Base.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));

            Label textLabel = new Label()
            {
                Text = path,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = true,
            };
            tbl_Base.Controls.Add(textLabel, 0, 0);

            ComboBox dropdown = new ComboBox()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill
            };
            dropdown.Items.AddRange(new string[] { "januari", "februari", "maart", "april", "mei", "juni", "juli", "augustus", "september", "oktober", "november", "december" });
            dropdown.Text = dropdown.Items[DateTime.Today.Month - 1].ToString();
            tbl_Base.Controls.Add(dropdown, 1, 0);

            NumericUpDown numBox = new NumericUpDown()
            {
                Minimum = 0,
                Maximum = 9999,
                Value = System.DateTime.Now.Year,
                Dock = DockStyle.Fill
            };
            tbl_Base.Controls.Add(numBox, 2, 0);

            Button bt_OK = new Button()
            {
                Text = "Confirm",
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                TabStop = false,
                DialogResult = DialogResult.OK
            };

            tbl_Base.Controls.Add(bt_OK, 2, 1);
            bt_OK.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(tbl_Base);

            prompt.AcceptButton = bt_OK;

            return prompt.ShowDialog() == DialogResult.OK ? (dropdown.Text + ' ' + numBox.Value.ToString()) : "CANCEL";
        }
    }
}
