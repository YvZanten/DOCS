using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace DOCS
{
    enum StateProgram
    {
        STATE_INI_READ,
        STATE_INI_NOTFOUND,
        STATE_INI_INCOMPLETE,
        STATE_USER_PATHS,
        STATE_SETUP_FILES,
        STATE_MOVE_FILES,
        STATE_EXIT
    }

    enum StateFileType
    {
        MEETING_WP,
        MEETING_IP,
        MEETING_PT,
        MEETING_DOC,
        MULTILINK_TDLMD,
        MULTILINK_ML,
        SINGLELINK_TDLSD,
        SINGLELINK_SL,
        UNKNOWN
    }

    public partial class FormProgress : Form
    {
        //path strings
        private string PathApplication;
        private string PathSource;
        private string PathDestination;

        //moved files list
        Dictionary<string, List<string>> MovedFiles;

        //list of files to move
        List<FileMove> FileList;

        //Controls for starting sorting process
        private Label label_ErrorInfo;
        private Label label_MoveInfo;
        private Button button_PathSource;
        private Button button_PathDestination;
        private Button button_SortFiles;
        private ProgressBar progress_MoveProgress;

        //identification string for generating folder paths
        private const string ID_TDLCAT_LAST = "TDLCAT_*";

        public FormProgress()
        {
            InitializeComponent();

            //set path strings
            PathApplication = Directory.GetCurrentDirectory();
            PathSource = "";
            PathDestination = "";

            //moved files list
            MovedFiles = new Dictionary<string, List<string>>();

            //create controls
            label_ErrorInfo = new Label
            {
                AutoSize = false,
                Size = new Size(panelContainer.Width, 0),
                Location = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };
            panelContainer.Controls.Add(label_ErrorInfo);

            label_MoveInfo = new Label
            {
                AutoSize = false,
                Size = new Size(panelContainer.Width, 25),
                Location = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };
            panelContainer.Controls.Add(label_MoveInfo);

            button_SortFiles = new Button
            {
                Text = "Sort files.",
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(panelContainer.Width, 25),
                Location = new Point(0, panelContainer.Height - 25),
                Padding = new Padding(0),
                Margin = new Padding(0),
                TabStop = false
            };
            panelContainer.Controls.Add(button_SortFiles);
            button_SortFiles.Click += button_SortFiles_Click;

            button_PathSource = new Button
            {
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(panelContainer.Width, (panelContainer.Height - label_ErrorInfo.Height - button_SortFiles.Height) / 2),
                Location = new Point(0, label_ErrorInfo.Height),
                Padding = new Padding(0),
                Margin = new Padding(0),
                TabStop = false
            };
            panelContainer.Controls.Add(button_PathSource);
            button_PathSource.Click += button_PathSource_Click;

            button_PathDestination = new Button
            {
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(panelContainer.Width, (panelContainer.Height - label_ErrorInfo.Height - button_SortFiles.Height) / 2),
                Location = new Point(0, button_PathSource.Location.Y + button_PathSource.Height),
                Padding = new Padding(0),
                Margin = new Padding(0),
                TabStop = false
            };
            panelContainer.Controls.Add(button_PathDestination);
            button_PathDestination.Click += button_PathDestination_Click;

            progress_MoveProgress = new ProgressBar
            {
                Maximum = 100,
                Minimum = 0,
                Size = button_SortFiles.Size,
                Location = button_SortFiles.Location,
                Value = 0,
                Visible = false
            };
            panelContainer.Controls.Add(progress_MoveProgress);
            
            StateMachine(StateProgram.STATE_INI_READ);
        }

        private void StateMachine(StateProgram state)
        {
            while (state != StateProgram.STATE_EXIT)
            {
                if (state == StateProgram.STATE_INI_READ)
                {
                    int result = INIread();

                    if (result == 0)
                        state = StateProgram.STATE_USER_PATHS;
                    else if (result == -1)
                        state = StateProgram.STATE_INI_NOTFOUND;
                    else if (result == -2)
                        state = StateProgram.STATE_INI_INCOMPLETE;
                }
                else if (state == StateProgram.STATE_INI_NOTFOUND)
                {
                    label_ErrorInfo.Text = "Created new settings.ini. Please select source and destination path.";
                    label_ErrorInfo.Size = new Size(panelContainer.Width, 25);
                    label_ErrorInfo.Visible = true;

                    state = StateProgram.STATE_USER_PATHS;
                }
                else if (state == StateProgram.STATE_INI_INCOMPLETE)
                {
                    label_ErrorInfo.Text = "settings.ini is incomplete, please select missing path(s).";
                    label_ErrorInfo.Size = new Size(panelContainer.Width, 25);
                    label_ErrorInfo.Visible = true;

                    state = StateProgram.STATE_USER_PATHS;
                }
                else if (state == StateProgram.STATE_USER_PATHS)
                {
                    if (PathSource.Length > 0)
                        button_PathSource.Text = "Source: " + PathSource;
                    else
                        button_PathSource.Text = "Select a source path.";

                    button_PathSource.Size = new Size(panelContainer.Width, (panelContainer.Height - label_ErrorInfo.Height - button_SortFiles.Height) / 2);
                    button_PathSource.Location = new Point(0, label_ErrorInfo.Height);

                    if (PathDestination.Length > 0)
                        button_PathDestination.Text = "Destination: " + PathDestination;
                    else
                        button_PathDestination.Text = "Select a destination path.";

                    button_PathDestination.Size = new Size(panelContainer.Width, (panelContainer.Height - label_ErrorInfo.Height - button_SortFiles.Height) / 2);
                    button_PathDestination.Location = new Point(0, button_PathSource.Location.Y + button_PathSource.Height);

                    state = StateProgram.STATE_EXIT;
                }
                else if (state == StateProgram.STATE_SETUP_FILES)
                {
                    //list of files to move
                    FileList = new List<FileMove>(); 

                    //get file list
                    string[] fileList = GetFileList(PathSource, new string[] { "pdf", "doc", "docx", "ppt", "pptx", "xls", "xlsx" });

                    //foreach file
                    string fileDestinationPath;

                    FileMove FM;

                    foreach (string file in fileList)
                    {
                        //get path
                        fileDestinationPath = GeneratePathForFile(file, PathDestination);

                        if (fileDestinationPath == "")
                            continue;

                        //create folders for path (if needed)
                        //fileDestinationPath = GeneratePathFolders(fileDestinationPath);
                        //if paths really exist
                        if (fileDestinationPath != "" && File.Exists(file))
                        {
                            FM = new FileMove(file, fileDestinationPath + '\\' + file.Split('\\').Last<string>(), GetFileType(file));

                            FileList.Add(FM);

                            /*if(Try_MoveFile(file, fileDestinationPath + '\\' + file.Split('\\').Last<string>()))
                            {
                                string typeName = FileTypeToString(GetFileType(file));

                                //add file to list of moved files
                                if (typeName != null && !MovedFiles.ContainsKey(typeName))
                                    MovedFiles.Add(typeName, new List<string>());

                                if (typeName != null)
                                    MovedFiles[typeName].Add(file);
                            }*/
                        }
                    }

                    state = StateProgram.STATE_MOVE_FILES;
                }
                else if (state == StateProgram.STATE_MOVE_FILES)
                {
                    MessageBox.Show("gona move it");

                    //enable progressbar
                    progress_MoveProgress.Visible = true;
                    progress_MoveProgress.BringToFront();
                    //progress_MoveProgress.Maximum = fileList.Length;
                    progress_MoveProgress.Value = 0;

                    //get order in which files should be moved
                    StateFileType[] MoveOrder = new StateFileType[9];
                    MoveOrder[0] = StateFileType.SINGLELINK_SL;
                    MoveOrder[1] = StateFileType.SINGLELINK_TDLSD;
                    MoveOrder[2] = StateFileType.MULTILINK_ML;
                    MoveOrder[3] = StateFileType.MULTILINK_TDLMD;
                    MoveOrder[4] = StateFileType.MEETING_WP;
                    MoveOrder[5] = StateFileType.MEETING_IP;
                    MoveOrder[6] = StateFileType.MEETING_PT;
                    MoveOrder[7] = StateFileType.MEETING_DOC;   //DOC last, so it is always sorted into newest meeting folder
                    MoveOrder[8] = StateFileType.UNKNOWN;

                    foreach(StateFileType type in MoveOrder)
                        for(int index = 0; index < FileList.Count; index++)
                            if(FileList[index].Type == type)
                            {
                                string move = FileList[index].MoveFile();
                                if(move != "")
                                    MessageBox.Show("EM:\r\n" + FileList[index].PathSrc + "\r\n\r\n" + FileList[index].PathDst + "\r\n\r\n" + move);

                                string typeName = FileTypeToString(GetFileType(FileList[index].PathSrc));

                                //add file to list of moved files
                                if (typeName != null && !MovedFiles.ContainsKey(typeName))
                                    MovedFiles.Add(typeName, new List<string>());

                                if (typeName != null)
                                    MovedFiles[typeName].Add(FileList[index].PathSrc);
                                
                                progress_MoveProgress.Value++;
                            }

                    //disable progressbar
                    progress_MoveProgress.SendToBack();
                    progress_MoveProgress.Visible = false;

                    LogWrite();

                    state = StateProgram.STATE_EXIT;
                }
            }
        }

        //read ini settings
        private int INIread()
        {
            //Check if file exists
            if (!File.Exists(PathApplication + "\\settings.ini"))
                return -1;

            //Read content
            string[] lines = File.ReadAllLines(PathApplication + "\\settings.ini");

            bool foundSection_Paths = false;
            bool foundKey_sSource = false;
            bool foundKey_sDestination = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (foundSection_Paths && lines[i][0] == '[')                           //wrong section
                    return -2;

                if (!foundSection_Paths && lines[i] != "[paths]")                       //right section
                    continue;
                else if (!foundSection_Paths && lines[i] == "[paths]")
                    foundSection_Paths = true;

                if(foundSection_Paths && lines[i].Split('=')[0] == "sSource")           //source
                {
                    PathSource = lines[i].Substring(lines[i].IndexOf('=') + 1);
                    foundKey_sSource = true;
                }

                if (foundSection_Paths && lines[i].Split('=')[0] == "sDestination")     //destination
                {
                    PathDestination = lines[i].Substring(lines[i].IndexOf('=') + 1);
                    foundKey_sDestination = true;
                }

                if (foundKey_sSource && foundKey_sDestination)                          //early end
                    break;
            }
            
            if (!(foundSection_Paths && foundKey_sSource && foundKey_sDestination))
                return -2;

            return 0;
        }

        //write ini settings
        private int INIwrite()
        {
            FileStream fs = File.Open(PathApplication + "\\settings.ini", FileMode.Create);

            string content = "[paths]\r\n" +
                "sSource=" + PathSource + "\r\n" + 
                "sDestination=" + PathDestination;

            fs.Write(Encoding.UTF8.GetBytes(content), 0, content.Length);

            fs.Close();

            return 0;
        }

        //write log to file
        private void LogWrite()
        {
            if (MovedFiles.Count == 0)
                return;
            
            //read existing content
            string content = "";
            if(File.Exists(PathApplication + "\\log.txt"))
                content = File.ReadAllText(PathApplication + "\\log.txt");
            
            //setup date time banner
            string log = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();
            string logBar = "#";
            for (int index = 0; index < 98; index++)
                logBar += '=';
            logBar += '#';

            log = logBar + "\r\n" + log + "\r\n" + logBar + "\r\n";
            
            //determine order of file types
            string[] LogTypeOrder = new string[7];
            LogTypeOrder[0] = FileTypeToString(StateFileType.SINGLELINK_TDLSD);      //same as StateFileType.SINGLELINK_SL
            LogTypeOrder[1] = FileTypeToString(StateFileType.MULTILINK_TDLMD);     //same as StateFileType.MULTILINK_ML
            LogTypeOrder[2] = FileTypeToString(StateFileType.MEETING_DOC);
            LogTypeOrder[3] = FileTypeToString(StateFileType.MEETING_IP);
            LogTypeOrder[4] = FileTypeToString(StateFileType.MEETING_PT);
            LogTypeOrder[5] = FileTypeToString(StateFileType.MEETING_WP);
            LogTypeOrder[6] = FileTypeToString(StateFileType.UNKNOWN);
            
            //get count of file types
            int[] LogTypeCount = new int[LogTypeOrder.Length];
            for (int index = 0; index < LogTypeCount.Length; index++)
                if (MovedFiles.ContainsKey(LogTypeOrder[index]))
                    LogTypeCount[index] = MovedFiles[LogTypeOrder[index]].Count;
            
            //show file type count at top of log
            for (int index = 0; index < LogTypeCount.Length; index++)
                log += LogTypeOrder[index] + " : " + LogTypeCount[index] + "\r\n";
            log += "\r\n";
            
            //get all values
            for (int indexType = 0; indexType < LogTypeOrder.Length; indexType++)
            {
                //get file type
                string type = LogTypeOrder[indexType];

                //if type is not found in movedfiles, continue
                if (!MovedFiles.ContainsKey(type))
                    continue;

                //add to log
                log += '\t' + type + " : " + MovedFiles[type].Count + "\r\n";
                foreach (string filename in MovedFiles[type])
                    log += "\t\t" + filename.Split('\\').Last<string>() + "\r\n";
            }
            
            //write content
            File.WriteAllText(PathApplication + "\\log.txt", log + "\r\n" + content);
            
            //clear moved files
            MovedFiles.Clear();
        }


        //return array with paths to all files in given folder with given extensions and set starts ("TDL-", "DOC-", "SL-", "ML")
        private string[] GetFileList(string pathSource, string[] fileTypes)
        {
            string[] DirectoryFiles = Directory.GetFiles(pathSource);
            List<string> files = new List<string>(DirectoryFiles.Length);

            //get all files starting with: "TDL-", "DOC-", "SL-", "ML" and of given filetypes
            for (int index = 0; index < files.Capacity; index++)
            {
                string fileName = DirectoryFiles[index].Split('\\').Last();
                string sub2 = fileName.Substring(0, 2);
                string sub3 = fileName.Substring(0, 3);
                string sub4 = fileName.Substring(0, 4);

                foreach (string type in fileTypes)
                    if (DirectoryFiles[index].Split('.').Last() == type && (sub4 == "TDL-" || sub4 == "DOC-" || sub3 == "SL-" || sub2 == "ML"))
                        files.Add(DirectoryFiles[index]);
            }

            files.TrimExcess();
            return files.ToArray();
        }

        //generates path for file
        private string GeneratePathForFile(string pathFile, string pathDestinationFolder)
        {
            string path = pathDestinationFolder;
            string fileName = pathFile.Split('\\').Last<string>();
            string[] fileNameSplit = fileName.Split('-');

            string MeetingNumber = "";
            string MultilinkNumber = "";
            string singlelinkNumber = "";

            StateFileType fileType = GetFileType(pathFile);

            switch(fileType)
            {
                case StateFileType.MEETING_WP:
                    MeetingNumber = fileNameSplit[2].Substring(1);
                    path += "\\" + MeetingNumber;
                    path += GetNumberRankingSuffix(MeetingNumber);
                    path += " TDL CaT - \\Working Paper";

                    break;

                case StateFileType.MEETING_IP:
                    MeetingNumber = fileNameSplit[2].Substring(1);
                    path += "\\" + MeetingNumber;
                    path += GetNumberRankingSuffix(MeetingNumber);
                    path += " TDL CaT - \\Information Paper";

                    break;

                case StateFileType.MEETING_PT:
                    MeetingNumber = fileNameSplit[2].Substring(1);
                    path += "\\" + MeetingNumber;
                    path += GetNumberRankingSuffix(MeetingNumber);
                    path += " TDL CaT - \\Presentation";

                    break;

                case StateFileType.MEETING_DOC:
                    string[] dirs = Directory.GetDirectories(path);
                    string LastTDLCAT = "";

                    foreach (string dir in dirs)
                        if (dir.Split('\\').Last().Contains(" TDL CaT - "))
                            LastTDLCAT = dir.Split('\\').Last();
                    
                    if (LastTDLCAT != "")
                        path += '\\' + LastTDLCAT + "\\Document";
                    else
                        path = "";

                    break;

                case StateFileType.MULTILINK_TDLMD:
                    MultilinkNumber = fileNameSplit[1].Substring(2, 3);
                    path += "\\Multi Link\\MD " + MultilinkNumber;

                    break;

                case StateFileType.SINGLELINK_TDLSD:
                    singlelinkNumber = fileNameSplit[2];
                    path += "\\Single Link\\SD " + singlelinkNumber;

                    break;

                case StateFileType.MULTILINK_ML:
                    MultilinkNumber = fileNameSplit[0].Substring(2, 3);
                    path += "\\Multi Link\\MD " + MultilinkNumber;

                    break;

                case StateFileType.SINGLELINK_SL:
                    singlelinkNumber = fileNameSplit[1];
                    path += "\\Single Link\\SD " + singlelinkNumber;

                    break;

                case StateFileType.UNKNOWN:
                    path += "";

                    break;

                default:
                    break;

            }

            return path;
        }

        //creates folder (if needed) for given path
        private string GeneratePathFolders(string filePath)
        {
            string[] pathSplit = filePath.Split('\\');
            string path = pathSplit[0];
            string TDLCaTMeetingFolder = "";

            //for each part of the path
            for(int index = 1; index < pathSplit.Length; index++)
            {
                //if path part is "xxth TDL CaT - "
                if(pathSplit[index].Contains(" TDL CaT - "))
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

        //gets filetype of given file
        private StateFileType GetFileType(string pathFile)
        {
            string[] fileNameSplit = pathFile.Split('\\').Last<string>().Split('-');
            int fileNamesplitLength = fileNameSplit.Length;

            if (fileNamesplitLength >= 4 && fileNameSplit[3].Length >= 2 && fileNameSplit[3].Substring(0, 2) == "WP")
                return StateFileType.MEETING_WP;

            else if (fileNamesplitLength >= 4 && fileNameSplit[3].Length >= 2 && fileNameSplit[3].Substring(0, 2) == "IP")
                return StateFileType.MEETING_IP;

            else if (fileNamesplitLength >= 4 && fileNameSplit[3].Length >= 2 && fileNameSplit[3].Substring(0, 2) == "PT")
                return StateFileType.MEETING_PT;

            else if (fileNamesplitLength >= 1 && fileNameSplit[0] == "DOC")
                return StateFileType.MEETING_DOC;

            else if (fileNamesplitLength >= 2 && fileNameSplit[0] == "TDL" && fileNameSplit[1].Length >= 2 && fileNameSplit[1].Substring(0, 2) == "MD")
                return StateFileType.MULTILINK_TDLMD;

            else if (fileNamesplitLength >= 1 && fileNameSplit[0].Substring(0, 2) == "ML")
                return StateFileType.MULTILINK_ML;

            else if (fileNamesplitLength >= 2 && fileNameSplit[0] == "TDL" && fileNameSplit[1] == "SD")
                return StateFileType.SINGLELINK_TDLSD;

            else if (fileNamesplitLength >= 1 && fileNameSplit[0] == "SL")
                return StateFileType.SINGLELINK_SL;

            else
                return StateFileType.UNKNOWN;
        }

        
        //tries to move file, gives error when it fails
        private bool Try_MoveFile(string source, string destination)
        {
            bool error = false;
            string errorMsg = "Errors have occured while trying to move a file:\r\n\t" + source + "\r\n\r\n";

            if(!File.Exists(source))
            {
                error = true;
                errorMsg += "ERROR: Could not find given source file!\r\n\r\n";
            }
            if (File.Exists(destination))
            {
                error = true;
                errorMsg += "ERROR: Destination path (file) already exists!\r\n\t" + destination + "\r\n\r\n";
            }

            try
            {
                if(!error)
                    File.Move(source, destination);
            }
            catch(Exception e)
            {
                error = true;
                errorMsg += "ERROR: " + e.GetType().ToString() + "\r\n" + e.Message + "\r\n\tSRC: " + source + "\r\n\tDST: " + destination;
            }

            if (error)
            {
                MessageBox.Show(errorMsg, "An error has occured!", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }

            return !error;
        }

        //returns 'st', 'nd', 'rd', or 'th' depending on number
        private string GetNumberRankingSuffix(string number)
        {
            if (number.Length > 1 && number.Substring(number.Length - 2) != "11" && number.Substring(number.Length - 1) == "1")
                return "st";
            else if (number.Length > 1 && number.Substring(number.Length - 2) != "12" && number.Substring(number.Length - 1) == "2")
                return "nd";
            else if (number.Length > 1 && number.Substring(number.Length - 2) != "13" && number.Substring(number.Length - 1) == "3")
                return "rd";
            else
                return "th";
        }

        //prompts user for TDL CAT folder date
        private string Prompt_TDLCaTFolderDate(string path)
        {
            Form prompt = new Form()
            {
                Width = 400 + SystemInformation.BorderSize.Width * 2,
                Height = 60 + (RectangleToScreen(this.ClientRectangle).Top - this.Top) + SystemInformation.BorderSize.Height * 2,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                Text = "Enter path date description",
                StartPosition = FormStartPosition.CenterScreen,
                MinimumSize = new Size(300, 60 + (RectangleToScreen(this.ClientRectangle).Top - this.Top) + SystemInformation.BorderSize.Height * 2),
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

        //returns user readable string version of filetype
        private string FileTypeToString(StateFileType type)
        {
            switch(type)
            {
                case StateFileType.MEETING_WP:
                    return "Working Paper";

                case StateFileType.MEETING_IP:
                    return "Information Paper";

                case StateFileType.MEETING_PT:
                    return "Presentation";

                case StateFileType.MEETING_DOC:
                    return "Document";

                case StateFileType.MULTILINK_TDLMD:
                    return "Multi Link";

                case StateFileType.MULTILINK_ML:
                    return "Multi Link";

                case StateFileType.SINGLELINK_TDLSD:
                    return "Single Link";

                case StateFileType.SINGLELINK_SL:
                    return "Single Link";

                case StateFileType.UNKNOWN:
                    return "Niet Verplaatst";

                default:
                    return null;
            }
        }


        //event methods
        private void button_PathSource_Click(object sender, EventArgs e)
        {
            folderDialogSource.ShowDialog();

            if (folderDialogSource.SelectedPath != "")
            {
                PathSource = folderDialogSource.SelectedPath;
                INIwrite();
            }

            StateMachine(StateProgram.STATE_USER_PATHS);
        }

        private void button_PathDestination_Click(object sender, EventArgs e)
        {
            folderDialogDestination.ShowDialog();

            if (folderDialogDestination.SelectedPath != "")
            {
                PathDestination = folderDialogDestination.SelectedPath;
                INIwrite();
            }

            StateMachine(StateProgram.STATE_USER_PATHS);
        }

        private void button_SortFiles_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(PathSource) || !Directory.Exists(PathDestination))
            {
                if (!Directory.Exists(PathSource))
                    PathSource = "";

                if (!Directory.Exists(PathDestination))
                    PathDestination = "";
                
                StateMachine(StateProgram.STATE_USER_PATHS);
                return;
            }

            StateMachine(StateProgram.STATE_SETUP_FILES);
        }
    }
}
