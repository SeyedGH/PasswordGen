using System;
using GlobalHotKey;
using System.Windows.Forms;
using System.IO;
using System.Windows.Input;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using System.Threading;
using DeviceId;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Microsoft.Win32;

namespace PasswordGen
{
    public partial class PasswordGen : Form
    {
        HotKeyManager hotKeyManager;
        HotKey hotKey;
        string deviceId;
        string historyPassword = null;
        TabPage tabPageHistory = null;

        public PasswordGen()
        {
            
            InitializeComponent();
            this.tabPageHistory = tabControl.TabPages["tabPageHistoryLive"];

            deviceId = new DeviceIdBuilder()
            .AddMachineName()
            .AddMacAddress()
            .OnWindows(windows => windows
                .AddProcessorId())
            .ToString();

        }

        private void RegisterShortcut()
        {
            try
            {
                hotKeyManager.Unregister(hotKey);
            }
            catch { }

            try
            {
                hotKeyManager = new HotKeyManager();

                string shortcut = textBoxShortcut.Text;

                var keys = shortcut.Replace(" ", "").Split('+');

                Key keypress;
                ModifierKeys mkeys1;
                ModifierKeys mkeys2;


                try
                {
                    if (keys.Length > 2)
                    {
                        Enum.TryParse(keys[0], out mkeys1);
                        Enum.TryParse(keys[1], out mkeys2);
                        Enum.TryParse(keys[2], out keypress);

                        hotKey = hotKeyManager.Register(keypress, mkeys1 | mkeys2);
                    }
                    else
                    {
                        Enum.TryParse(keys[0], out mkeys1);
                        Enum.TryParse(keys[1], out keypress);

                        hotKey = hotKeyManager.Register(keypress, mkeys1);
                    }
                }
                catch { MessageBox.Show("Can not register keys!"); }

                hotKeyManager.KeyPressed += HotKeyManagerPressed;

                buttonGenerate.Text = "Generate (" + textBoxShortcut.Text + ")";
            }
            catch { }
        }

        private void HotKeyManagerPressed(object sender, KeyPressedEventArgs e)
        {
            if (textBoxShortcut.Focused != true)
                BuildPassword();
        }

        private void PasswordGen_Resize(Object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon.Visible = true;
            }
        }

        private void button_Click(object sender, EventArgs e)
        {
            BuildPassword();
        }


        public void BuildPassword()
        {
            if (textLastGenerated.InvokeRequired)
            {
                try
                {
                    Action safeWrite = delegate { BuildPassword(); };
                    textLastGenerated.Invoke(safeWrite);
                }
                catch { }
            }
            else
            {
                try
                {
                    string symbols;
                    string numeric;
                    string higherCases;
                    string lowerCases;
                    int length;
                    int symbolLength;
                    int numericLength;
                    bool useSymbols;
                    bool useNumeric;
                    bool useHigherCases;
                    bool useLowerCases;

                    symbols = textSymbols.Text;
                    numeric = textNumbers.Text;
                    higherCases = textHigherCase.Text;
                    lowerCases = textLowerCase.Text;
                    length = Convert.ToInt32(numericUpDownLength.Value);
                    symbolLength = Convert.ToInt32(numericUpDownSymbols.Value);
                    numericLength = Convert.ToInt32(numericUpDownNumbers.Value);

                    useSymbols = checkBoxSymbols.Checked;
                    useHigherCases = checkBoxHigherCase.Checked;
                    useLowerCases = checkLowerCase.Checked;
                    useNumeric = checkBoxNumbers.Checked;

                    if (useSymbols == false)
                    {
                        symbols = "";
                        symbolLength = 0;
                    }

                    if (useNumeric == false)
                    {
                        numeric = "";
                        numericLength = 0;
                    }

                    if (useHigherCases == false)
                    {
                        higherCases = "";
                    }

                    if (useLowerCases == false)
                    {
                        lowerCases = "";
                    }

                    string createdPassword = CreatePassword(length, symbols, symbolLength, numeric, numericLength, higherCases, lowerCases); ;

                    if (checkBoxClipboardCopy.Checked)
                    {
                        ClearClipboard(createdPassword);
                    }

                    textLastGenerated.Text = createdPassword;

                    if (checkBoxBaloonTip.Checked)
                    {
                        ToastMessage(createdPassword);
                    }

                    if (checkBoxHistory.Checked)
                    {
                        listBoxHistory.Items.Insert(0,createdPassword);

                        if (listBoxHistory.Items.Count > numericUpDownHistoryCount.Value )
                        {
                            listBoxHistory.Items.RemoveAt(Convert.ToInt32(numericUpDownHistoryCount.Value));
                        }
                    }

                    writeConfig();
                }
                catch { }
                
            }
                
        }

        private void ClearClipboard(string createdPassword)
        {
            Clipboard.SetText(createdPassword);
            timerClearClip.Enabled = false;
            timerClearClip.Interval = Convert.ToInt32(numericUpDowClearClipSeconds.Value) * 1000;
            timerClearClip.Enabled = true;
        }

        private void ToastMessage(string createdPassword)
        {
            try
            {
                Thread.Sleep(150);
                ToastNotificationManager.History.Remove("Password-Gen");

            }
            catch { }

            ToastDuration ToastDuration = ToastDuration.Short;

            if (this.radioButtonToastLong.Checked == true)
            {
                ToastDuration = ToastDuration.Long;

            }

            new ToastContentBuilder()
                .AddArgument("action", "archive")
                .AddText(createdPassword)
                .SetToastDuration(ToastDuration)
                .Show(toast =>
                {
                    toast.Tag = "Password-Gen";
                });
        }

        private static string Shuffle(string str)
        {
            char[] array = str.ToCharArray();
            Random rng = new Random();
            int n = array.Length;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                var value = array[k];
                array[k] = array[n];
                array[n] = value;
            }
            return new string(array);
        }

        private string CreatePassword(int length, string symbols, int symbollength, string numbers, int numberslength, string highercase, string lowercase)
        {
            try
            {
                string validChars = symbols;
                Random random = new Random();

                char[] chars = new char[symbollength];
                for (int i = 0; i < symbollength; i++)
                {
                    chars[i] = validChars[random.Next(0, validChars.Length)];
                }

                string passwordSymbols = new string(chars);

                validChars = numbers;
                random = new Random();

                chars = new char[numberslength];
                for (int i = 0; i < numberslength; i++)
                {
                    chars[i] = validChars[random.Next(0, validChars.Length)];
                }

                string passwordNumbers = new string(chars);

                validChars = highercase + lowercase;
                random = new Random();
                int newlength = length - symbollength - numberslength;

                chars = new char[newlength];
                for (int i = 0; i < newlength; i++)
                {
                    chars[i] = validChars[random.Next(0, validChars.Length)];
                }

                string passwordAlpha = new string(chars);

                string returnPass = "";


                if (radioButtonSymRandom.Checked && radioButtonNumRandom.Checked)
                    returnPass =  Shuffle(passwordSymbols + passwordNumbers + passwordAlpha);
                
                else if  (radioButtonSymRandom.Checked && radioButtonNumFront.Checked)
                    returnPass = passwordNumbers + Shuffle(passwordSymbols + passwordAlpha);

                else if (radioButtonSymRandom.Checked && radioButtonNumBack.Checked)
                    returnPass = Shuffle(passwordSymbols + passwordAlpha) + passwordNumbers;

                else if (radioButtonSymFront.Checked && radioButtonNumRandom.Checked)
                    returnPass = passwordSymbols + Shuffle(passwordNumbers + passwordAlpha) ;

                else if (radioButtonSymBack.Checked && radioButtonNumRandom.Checked)
                    returnPass = Shuffle(passwordNumbers + passwordAlpha) + passwordSymbols;

                else if (radioButtonSymBack.Checked && radioButtonNumFront.Checked)
                    returnPass = passwordNumbers + passwordAlpha + passwordSymbols;
                
                else if (radioButtonSymFront.Checked && radioButtonNumBack.Checked)
                    returnPass = passwordSymbols + passwordAlpha + passwordNumbers;

                else if (radioButtonSymFront.Checked && radioButtonNumBack.Checked)
                    returnPass = passwordSymbols + passwordAlpha + passwordNumbers;

                else if (radioButtonSymFront.Checked && radioButtonNumFront.Checked)
                    returnPass = passwordSymbols + passwordNumbers + passwordAlpha ;
                
                else if (radioButtonSymBack.Checked && radioButtonNumBack.Checked)
                    returnPass =  passwordAlpha + passwordNumbers + passwordSymbols;
               
                return returnPass;  

            }
            catch 
            {
                return "";
            }
            

        }
        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                Hide();
                this.WindowState = FormWindowState.Minimized;
            }
            else
            {
                Show();
                this.WindowState = FormWindowState.Normal;
            }
            
        }
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                hotKeyManager.Dispose();
            }catch { }
            
            Application.Exit();

        }

        private void PasswordGen_Load(object sender, EventArgs e)
        {
            readConfig();

        }

        private string Encrypt(string secureUserData, bool useHashing)
        {
            byte[] keyArray;
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(secureUserData);
            string key = string.Empty;
            byte[] resultArray;

            key = deviceId;

            if (useHashing)
            {
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
                hashmd5.Clear();
}
            else
            {
                keyArray = UTF8Encoding.UTF8.GetBytes(key);
            }

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateEncryptor();

            resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            tdes.Clear();

            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public string Decrypt(string cipherString, bool useHashing)
        {
            byte[] keyArray;
            byte[] toEncryptArray = Convert.FromBase64String(cipherString);
            byte[] resultArray;
            string key = string.Empty;

            key = deviceId;

            if (useHashing)
            {
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
                hashmd5.Clear();
            }
            else
            {
                keyArray = UTF8Encoding.UTF8.GetBytes(key);
            }

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            tdes.Clear();

            return UTF8Encoding.UTF8.GetString(resultArray);
        }

        private void writeConfig()
        {
            try
            {
                string symbols;
                string numeric;
                string higherCases;
                string lowerCases;
                string length;
                string symbolLength;
                string numericLength;
                string useSymbols;
                string useNumeric;
                string useHigherCases;
                string useLowerCases;
                string passwordHistory;
                string balloonTip;
                string balloonTipDuration;
                string opacity;
                string opacityPercentage;
                string clipboardCopy;
                string clipboardCopyDureation;
                string history;
                string historyAmount;
                string shortcut;
                string shortcutKeys;
                string symbolPlacement = "";
                string numberPlacement = "";
                string topMost = "";

                symbols = textSymbols.Text;
                numeric = textNumbers.Text;
                higherCases = textHigherCase.Text;
                lowerCases = textLowerCase.Text;
                length = Convert.ToString(numericUpDownLength.Value);
                symbolLength = Convert.ToString(numericUpDownSymbols.Value);
                numericLength = Convert.ToString(numericUpDownNumbers.Value);
                useSymbols = Convert.ToString(checkBoxSymbols.Checked);
                useHigherCases = Convert.ToString(checkBoxHigherCase.Checked);
                useLowerCases = Convert.ToString(checkLowerCase.Checked);
                useNumeric = Convert.ToString(checkBoxNumbers.Checked);
                passwordHistory = string.Join("#;;;#;;;#", listBoxHistory.Items.OfType<object>());

                balloonTip = Convert.ToString(checkBoxBaloonTip.Checked);
                balloonTipDuration = Convert.ToString(radioButtonToastShort.Checked);

                opacity = Convert.ToString(checkBoxOpacity.Checked);
                opacityPercentage = Convert.ToString(numericUpDownOpacityPercent.Value);

                clipboardCopy = Convert.ToString(checkBoxClipboardCopy.Checked);
                clipboardCopyDureation = Convert.ToString(numericUpDowClearClipSeconds.Value);

                history = Convert.ToString(checkBoxHistory.Checked);
                historyAmount = Convert.ToString(numericUpDownHistoryCount.Value);

                shortcut = Convert.ToString(checkBoxShortcut.Checked);
                shortcutKeys = Convert.ToString(textBoxShortcut.Text);

                topMost = Convert.ToString(checkBoxTopMost.Checked);



                if (radioButtonSymRandom.Checked)
                    symbolPlacement = "Random";
                
                else if (radioButtonSymFront.Checked)
                    symbolPlacement = "Front";

                else if (radioButtonSymBack.Checked)
                    symbolPlacement = "Back";

                if (radioButtonNumRandom.Checked)
                    numberPlacement = "Random";

                else if (radioButtonNumRandom.Checked)
                    numberPlacement = "Front";

                else if (radioButtonNumRandom.Checked)
                    numberPlacement = "Back";

                var historyBytes = System.Text.Encoding.UTF8.GetBytes(Encrypt(passwordHistory,true));
                string history64 = System.Convert.ToBase64String(historyBytes);

                string config = symbols + "%||%" + numeric + "%||%" + higherCases + "%||%" + lowerCases + "%||%" + length + "%||%" + symbolLength + "%||%" + numericLength + "%||%" +
                                useSymbols + "%||%" + useNumeric + "%||%" + useHigherCases + "%||%" + useLowerCases + "%||%" + history64 + "%||%" + historyPassword + "%||%" +
                                balloonTip + "%||%" + balloonTipDuration + "%||%" + opacity + "%||%" + opacityPercentage + "%||%" + clipboardCopy + "%||%" + clipboardCopyDureation + "%||%" +
                                history + "%||%" + historyAmount + "%||%" + shortcut + "%||%" + shortcutKeys + "%||%" + symbolPlacement + "%||%" + numberPlacement + "%||%" + topMost;

                var configBytes = System.Text.Encoding.UTF8.GetBytes(config);
                string config64 = System.Convert.ToBase64String(configBytes);

                File.WriteAllText(Path.GetDirectoryName(Application.ExecutablePath) + @"\config.pwgen", config64);

            }
            catch { }
        }

        private void readConfig()
        {
            try
            {
                if (File.Exists(Path.GetDirectoryName(Application.ExecutablePath) + @"\config.pwgen") == false)
                {
                    MessageBox.Show("This application is a tray application!", "Tray application", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    writeConfig();
                }
                    

                string config64 = File.ReadAllText(Path.GetDirectoryName(Application.ExecutablePath) + @"\config.pwgen");

                string config;
                byte[] data = System.Convert.FromBase64String(config64);
                config = System.Text.ASCIIEncoding.ASCII.GetString(data);

                string[] configitems = config.Split(new string[] { "%||%" }, StringSplitOptions.None);

                string symbols = configitems[0];
                string numeric = configitems[1];
                string higherCases = configitems[2];
                string lowerCases = configitems[3];
                int length = Convert.ToInt32(configitems[4]);
                int symbolLength = Convert.ToInt32(configitems[5]);
                int numericLength = Convert.ToInt32(configitems[6]);
                bool useSymbols = Convert.ToBoolean(configitems[7]);
                bool useHigherCases = Convert.ToBoolean(configitems[8]);
                bool useLowerCases = Convert.ToBoolean(configitems[9]); 
                bool useNumeric = Convert.ToBoolean(configitems[10]);

                byte[] dataHistory = System.Convert.FromBase64String(configitems[11].ToString());

                string[] passwordHistory = new string[0];

                try
                {
                    passwordHistory = Decrypt(System.Text.ASCIIEncoding.ASCII.GetString(dataHistory), true).Split(new string[] { "#;;;#;;;#" }, StringSplitOptions.None);

                }
                catch { }


                if (configitems[12] == "")
                    historyPassword = null;
                else
                    historyPassword = configitems[12];

                bool balloonTip = Convert.ToBoolean(configitems[13]); 
                bool balloonTipDuration = Convert.ToBoolean(configitems[14]); 
                bool opacity = Convert.ToBoolean(configitems[15]);
                int opacityPercentage = Convert.ToInt32(configitems[16]);
                bool clipboardCopy = Convert.ToBoolean(configitems[17]);
                int clipboardCopyDureation = Convert.ToInt32(configitems[18]);
                bool history = Convert.ToBoolean(configitems[19]);
                int historyAmount = Convert.ToInt32(configitems[20]);
                bool shortcut = Convert.ToBoolean(configitems[21]);
                string shortcutKeys = Convert.ToString(configitems[22]);
                string symbolPlacement = Convert.ToString(configitems[23]);
                string numberPlacement = Convert.ToString(configitems[24]);
                bool  topMost = Convert.ToBoolean(configitems[25]);

                textSymbols.Text = symbols;
                textNumbers.Text = numeric;
                textHigherCase.Text = higherCases;
                textLowerCase.Text = lowerCases;
                numericUpDownLength.Value = length;
                numericUpDownSymbols.Value = symbolLength;
                numericUpDownNumbers.Value = numericLength;
                checkBoxSymbols.Checked = useSymbols;
                checkBoxHigherCase.Checked = useHigherCases;
                checkLowerCase.Checked = useLowerCases;
                checkBoxNumbers.Checked = useNumeric;
                checkBoxTopMost.Checked = topMost;

                listBoxHistory.Items.Clear();

                for (int i = 0; i < passwordHistory.Length; i++)
                {
                    listBoxHistory.Items.Add(passwordHistory[i].ToString());
                }

                checkBoxBaloonTip.Checked = balloonTip;
                if (balloonTipDuration)
                    radioButtonToastShort.Checked = true;
                else
                    radioButtonToastLong.Checked = true;

                checkBoxOpacity.Checked = opacity;
                numericUpDownOpacityPercent.Value = opacityPercentage;

                checkBoxClipboardCopy.Checked = clipboardCopy;
                numericUpDowClearClipSeconds.Value = clipboardCopyDureation;

                checkBoxHistory.Checked = history;
                numericUpDownHistoryCount.Value = historyAmount;

                checkBoxShortcut.Checked = shortcut;
                textBoxShortcut.Text = shortcutKeys;

                if (shortcut)
                    RegisterShortcut();

                if (symbolPlacement == "Random")
                    radioButtonSymRandom.Checked = true;
                else if (symbolPlacement =="Front")
                    radioButtonSymFront.Checked = true;
                else if (symbolPlacement == "Back")
                    radioButtonSymBack.Checked = true;

                if (numberPlacement == "Random")
                    radioButtonNumRandom.Checked = true;
                else if (numberPlacement == "Front")
                    radioButtonNumRandom.Checked = true;
                else if (numberPlacement == "Back")
                    radioButtonNumRandom.Checked = true;

                if (checkBoxTopMost.Checked)
                    this.TopMost = true;
                else 
                    this.TopMost = false;


                RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);

                if (rkApp.GetValue("PasswordGen") == null)
                {
                    checkBoxAutoStart.Checked = false;
                }
                else
                {
                    checkBoxAutoStart.Checked = true;
                }
            }
            catch { }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            Hide();
            timer.Enabled = false;
        }

        private void PasswordGen_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                this.Hide();
                e.Cancel = true;
            }
        }

        private void PasswordGen_Activated(object sender, EventArgs e)
        {
            this.Opacity = 1.0;
        }

        private void PasswordGen_Deactivate(object sender, EventArgs e)
        {
            try
            {
                if (checkBoxOpacity.Checked)
                {
                    this.Opacity = Convert.ToDouble(numericUpDownOpacityPercent.Value) / 100;

                }
            }
            catch { }


        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            writeConfig();
        }

        private void buttonGenerate_Click(object sender, EventArgs e)
        {
            BuildPassword();
        }

        private void timerClearClip_Tick(object sender, EventArgs e)
        {
            try
            {
                if (Clipboard.GetText() == textLastGenerated.Text)
                {
                    Clipboard.Clear();
                    textLastGenerated.Text = "";
                }

                timerClearClip.Enabled = false;
            }
            catch { }


        }

        private void textBoxShortcut_Leave(object sender, EventArgs e)
        {
            RegisterShortcut();
        }

        private void textBoxShortcut_Enter(object sender, EventArgs e)
        {
            try
            {
                hotKeyManager.Unregister(hotKey);
            }catch { }  
            

        }

        private void listBoxHistory_DoubleClick(object sender, EventArgs e)
        {
            var password = listBoxHistory.SelectedItem.ToString();

            ToastMessage(password);
            ClearClipboard(password);
        }

        private void textBoxHistoryPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (historyPassword == null)
                {
                    StartSetPassword();

                }
                else
                {
                    byte[] hashBytes = Convert.FromBase64String(historyPassword);
                    byte[] salt = new byte[16];
                    Array.Copy(hashBytes, 0, salt, 0, 16);
                    var pbkdf2 = new Rfc2898DeriveBytes(textBoxHistoryPassword.Text, salt, 100);
                    byte[] hash = pbkdf2.GetBytes(20);
                    for (int i = 0; i < 20; i++)
                        if (hashBytes[i + 16] != hash[i])
                        {
                            textBoxHistoryPassword.Text = "";
                        }
                        else
                        {
                            ShowHistoryPasswords();
                        }
                }

                e.Handled = true;
                e.SuppressKeyPress = true;

            }
        }

        private void StartSetPassword()
        {
            
            if (textBoxHistoryPassword.Text == "")
            {
                DialogResult dr = MessageBox.Show("Really? Do you want to really set no password for this?", "No password", MessageBoxButtons.YesNo);

                switch (dr)
                {
                    case DialogResult.Yes:
                        SetPassword();
                        break;
                    case DialogResult.No:
                        break;
                }

            }
            else
            {
                SetPassword();

            }
            
        }

        private void SetPassword()
        {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);
            var pbkdf2 = new Rfc2898DeriveBytes(textBoxHistoryPassword.Text, salt, 100);
            byte[] hash = pbkdf2.GetBytes(20);
            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);

            historyPassword = Convert.ToBase64String(hashBytes);

            writeConfig();
            ShowHistoryPasswords();
        }

        private void ShowHistoryPasswords()
        {
            textBoxHistoryPassword.Text = "";
            panelHistoryPassword.Visible = false;
            timerClearClip.Interval = Convert.ToInt32(300) * 1000;
            timerHistoryLock.Enabled = true;
        }
        private void buttonResetHistoryPassword_Click(object sender, EventArgs e)
        {

            DialogResult dr = MessageBox.Show("Your old history get's wiped, do you want to proceed?",
                      "Reset History Password", MessageBoxButtons.YesNo);
            switch (dr)
            {
                case DialogResult.Yes:
                    listBoxHistory.Items.Clear();

                    StartSetPassword();

                    break;
                case DialogResult.No:
                    break;
            }
        }

        private void timerHistoryLock_Tick(object sender, EventArgs e)
        {
            try
            {
                panelHistoryPassword.Visible = true;
                timerHistoryLock.Enabled = false;
            }
            catch { }
        }

        private void buttonSave2_Click(object sender, EventArgs e)
        {
            writeConfig();
            readConfig();
        }

        private void checkBoxHistory_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxHistory.Checked == true)
            {
                this.tabControl.TabPages.Insert(2,this.tabPageHistory);
                panelHistoryPassword.Visible = true;
            }
            else
            {
                this.tabControl.TabPages.Remove(this.tabPageHistory);
                this.timerHistoryLock.Enabled = false;
            }

        }

        private void checkBoxShortcut_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxShortcut.Checked == true)
            {
                RegisterShortcut();
            }
            else
            {
                try
                {
                    hotKeyManager.Dispose();
                }catch { }
            }
        }
        private void pictureBoxDonate_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.paypal.com/donate/?hosted_button_id=VQ2LF9LU8C5VJ");
        }

        private void checkBoxAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            RegistryKey autostartReg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (checkBoxAutoStart.Checked)
            {
                autostartReg.SetValue("PasswordGen", Application.ExecutablePath);
            }
            else
            {
                autostartReg.DeleteValue("PasswordGen", false);
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://passwordgen.info");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
        

            System.Diagnostics.Process.Start("https://sourceforge.net/projects/passwordgenerator");
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/SeyedGH/PasswordGen");
        }

        private void textBoxShortcut_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void textBoxShortcut_KeyDown(object sender, KeyEventArgs e)
        {

            string[] keyData = e.KeyData.ToString().Split(',');

            Array.Reverse(keyData);

            textBoxShortcut.Text = "";

            foreach (string keypress  in  keyData  )
            {
                if (keypress.IndexOf("Key") > 0)
                {
                    continue;

                }
                    

                if (textBoxShortcut.Text != "")
                    textBoxShortcut.Text += " + ";

                textBoxShortcut.Text += keypress.Trim();

            }
           
        }
    }
}
