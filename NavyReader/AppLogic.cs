using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using Universe.Utility;
using System.Linq;
using System.Collections.Generic;
using NFT.NavyReader.ref1;
using Newtonsoft.Json;
using System.IO;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace NFT.NavyReader
{
    using SI = MySendInput;
    using KL = List<(Keys key, IntPtr wp)>;
    using KF = ValueTuple<Dictionary<Groth, string>, Dictionary<Groth, List<(Keys key, IntPtr wp)>>>;
    using GI = Dictionary<Groth, int>;

    internal class AppLogic : IDisposable
    {
        #region ---- UI ----

        Action<object> _logger;
        public AppLogic(Action<object> logger)
        {
            _logger = logger;
            _acts = new GI();
            _address = new GI() { { Groth.G잠재, 0x2D0ED0D0 } };

            loadNames();
            loadSels();
            initProcess();
        }
        void log(object msg) => _logger?.Invoke(msg);

        #endregion


        #region ---- Selection ----

        static string _selFile = "sels.json";
        Dictionary<Groth, bool> _sels;
        public void AddSel(Groth g, bool sel) => _sels[g] = sel;
        public bool GetSel(Groth g) => _sels.ContainsKey(g) && _sels[g];

        void saveSels()
        {
            log($"Saving sels: {_selFile}");
            string json = JsonConvert.SerializeObject(_sels, Formatting.Indented);
            File.WriteAllText(_selFile, json);
        }
        void loadSels()
        {
            if (File.Exists(_selFile))
            {
                log($"Loading sels: {_selFile}");
                var text = File.ReadAllText(_selFile);
                _sels = JsonConvert.DeserializeObject<Dictionary<Groth, bool>>(text);
            }
            else _sels = new Dictionary<Groth, bool>();
        }
        #endregion


        #region ---- Expected ----

        static string _expFile = "check.json";
        Dictionary<Groth, (int priValue, GI secValue)> _exps;
        void saveExps()
        {
            log($"Saving exps: {_expFile}");
            string json = JsonConvert.SerializeObject(_exps, Formatting.Indented);
            File.WriteAllText(_expFile, json);
        }
        void loadExps()
        {
            if (File.Exists(_expFile))
            {
                log($"Loading exps: {_expFile}");
                var text = File.ReadAllText(_expFile);
                _exps = JsonConvert.DeserializeObject<Dictionary<Groth, (int priValue, GI secValue)>>(text);
            }
            else _exps = new Dictionary<Groth, (int priValue, GI secValue)>();
        }

        #endregion


        #region ---- Names ----

        Dictionary<Groth, KL> _nameKeys;
        Dictionary<Groth, string> _nameText;
        static string _namesFile = "names.json";

        public void AddNameKey(Groth g, string text, KL key)
        {
            _nameText[g] = text;
            _nameKeys[g] = key;
        }
        public string GetNameText(Groth g) => _nameText.ContainsKey(g) ? _nameText[g] : "";

        void saveNames()
        {
            log($"Saving names: {_namesFile}");
            string json = JsonConvert.SerializeObject((_nameText, _nameKeys), Formatting.Indented);
            File.WriteAllText(_namesFile, json);
        }
        void loadNames()
        {
            log($"Loading names: {_namesFile}");
            if (File.Exists(_namesFile))
            {
                var tuple = JsonConvert.DeserializeObject<KF>(File.ReadAllText(_namesFile));
                _nameText = tuple.Item1;
                _nameKeys = tuple.Item2;
            }
            else
            {
                _nameKeys = new Dictionary<Groth, KL>();
                _nameText = new Dictionary<Groth, string>();
            }
        }

        #endregion


        #region ---- Process PInvoke ----

        GI _address;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetCursorPos(int x, int y);
        void SetCursor(int x, int y)
        {
            if (!SetCursorPos(x, y))
            {
                var error = Marshal.GetLastWin32Error();
                log($"SetCursor() error: code= {error}");
            }

        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);
        byte[] ReadProcessMemory(int address, int size)
        {
            byte[] buffer = new byte[size];
            var ipAddress = new IntPtr(address);
            var result = ReadProcessMemory(_handle, ipAddress, buffer, size, out var numBytes);
            if (!result || numBytes != size) throw new Exception($"ReadProcessMemory() failed: result={result}, numBytes={numBytes}");
            return buffer;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);


        Process _process;
        IntPtr _handle;
        public Process Process => _process;
        void initProcess()
        {
            var appName = "NavyFIELD";
            _process = Process.GetProcessesByName(appName)[0];
            _handle = OpenProcess(ProcessAccessFlags.PROCESS_VM_READ, false, _process.Id);
            log($"app= {appName}, process handle= 0x{_handle:X}, main window title= {_process.MainWindowTitle}");
        }

        public void Dispose()
        {
            if (_handle != null) CloseHandle(_handle);
        }
        public void SaveConfig()
        {
            saveSels();
            saveExps();
            saveNames();
        }

        #endregion


        #region ---- Run ----

        GI _acts;
        Point _origin;
        Point _newButton = new Point(895, 720);
        Point _renewButton = new Point(665, 420);
        Point _groth = new Point(435, 243);
        Ocr _ocr;
        static double _desktopRatio = 1.0;
        (int x, int y) _grothSize = (20, 150);

        volatile bool _running = false;
        ManualResetEvent _exitEvent = new ManualResetEvent(true);

        public async Task Toggle() => await Toggle(!_running);
        public async Task Toggle(bool start)
        {
            if (_running)//실행중
            {
                if (start) return;
                log("Stopping...");
                _running = false;
                await _exitEvent.WaitOneAsync();
            }
            else
            {
                if (!start) return;
                log("Starting...");
                _running = true;
                _exitEvent.Reset();
                Task.Run(run);
            }
        }

        volatile int _runCounter = 0;
        (int sx, int xy) _imgStart;
        (int w, int h) _imgSize;
        void run()
        {
            Thread.Sleep(100);
            WindowHelper.BringMainWindowToFront(_process);
            _origin = WindowHelper.GetWindowPosition(_process);
            _imgStart = ((int)((_origin.X + _groth.X) * _desktopRatio), (int)((_origin.Y + _groth.Y) * _desktopRatio));
            _imgSize = ((int)(_grothSize.x * _desktopRatio), (int)(_grothSize.y * _desktopRatio));

            saveSels();
            saveNames();
            loadExps();

            Thread.Sleep(100);
            click(_newButton);
            Thread.Sleep(100);
            _runCounter = 0;

            while (_running)
            {
                //update(4, ++counter);
                _runCounter++;

                if (!_running) break;
                Thread.Sleep(800);

                try
                {
                    read();
                }
                catch (Exception ex)
                {
                    log(ex.Message);
                    //break;//*************************************************************************
                    SendKeys.SendWait("{ESC}");
                    click(_newButton);
                    continue;
                }

                var r = check2();
                if (r != Groth.None)
                {
                    save(r);
                    Thread.Sleep(1000);
                    click(_newButton);
                }
                else click(_renewButton);

                if (!_running) break;
                //Thread.Sleep(500);

                //return;
            }

            log("Exiting Run()...");
            _exitEvent.Set();
        }

        void click(Point clientPoint)
        {
            //Cursor.Position = new Point(_origin.X + clientPoint.X, _origin.Y + clientPoint.Y);
            SetCursor(_origin.X + clientPoint.X, _origin.Y + clientPoint.Y);
            log($"cursor= ({Cursor.Position})");
            Thread.Sleep(100);
            SI.SendMouseLeft();

            //SI.SendMouseLeft(_origin.X + clientPoint.X, _origin.Y + clientPoint.Y);
        }

        StringBuilder _sb = new StringBuilder();
        void read()
        {
            using (_ocr = new Ocr())
            {
                var image = _ocr.Capture(_imgStart, _imgSize);
                var result = _ocr.Process(image, 220);
                var text = result.text.Trim().Replace(" ", "").Replace('\n', ' ');
                //log($"[{_runCounter}] ocr: {text}");

                var nums = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToArray();

                //_sb.Clear();
                //_sb.Append($"[{_runCounter,5}]");
                //foreach (var n in nums) _sb.Append($"{n,03:D2}");
                //log(_sb.ToString());
                var isError = nums.Length != 11;
                for (int i = 1; i < nums.Length; i++) isError |= (nums[i] > 12);
                isError |= nums.Length > 0 && nums[0] > 15;
                if (isError)
                {
                    var time = DateTime.Now.ToString("HHmmss.f");
                    result.image.Save($"{time}_c.png", ImageFormat.Png);
                    result.imgBw.Save($"{time}_b.png", ImageFormat.Png);

                    _sb.Clear();
                    _sb.Append($"[{_runCounter,5}] OCR Error:");
                    foreach (var n in nums) _sb.Append($"{n,03:D2}");
                    throw new Exception(_sb.ToString());
                }
                else for (int i = 0; i < nums.Length; i++) _acts[(Groth)i] = nums[i];
            }
            //log("exiting read()...");
        }
        void readMemory()
        {
            foreach (var g in _address.Keys)
            {
                var buffer = ReadProcessMemory(_address[g], 4);
                _acts[g] = BitConverter.ToInt32(buffer, 0);
            }
        }

        Groth check2()
        {
            //log("starting check2()... ");
            var ret = Groth.None;
            foreach (var g in _exps.Keys)
            {
                if (!_sels[g]) continue;
                if (_acts[g] < _exps[g].priValue) continue;
                var result = true;
                foreach (var g2 in _exps[g].secValue.Keys) result &= (_acts[g2] >= _exps[g].secValue[g2]);
                //if (result) return g;
                ret = result ? g : Groth.None;
            }
            //log($"quiting check2() : {ret}");
            return ret;
        }
        void save(Groth g)
        {
            log($"saving {_nameText[g]} = {_acts[g]}");
            _sb.Clear();
            _sb.Append($"[{_runCounter,5}]");
            foreach (var n in _acts.Values) _sb.Append($"{n,03:D2}");
            log(_sb.ToString());

            var kp = new KeysPlayer(_nameKeys[g]);
            kp.Start();
            SendKeys.SendWait("{ENTER}");
        }

        public void Test()
        {
            readMemory();
            return;

            saveSels();
            saveNames();

            buildExps();
            saveExps();
            //loadExps();
            buildActs();

            var r = check2();
            log(r);

            void buildActs()
            {
                var gs = (Groth[])Enum.GetValues(typeof(Groth));
                foreach (var g in gs) if (g != Groth.None) _acts[g] = 9;

                //_acts[Groth.G명중] = 12;
                //_acts[Groth.G연사] = 11;

                //_acts[Groth.G함재] = 12;
                //_acts[Groth.G전투] = 10;
                //_acts[Groth.G폭격] = 10;
            }
            void buildExps()
            {
                _exps = new Dictionary<Groth, (int priValue, GI secValue)>();
                _exps[Groth.G잠재] = (15, new GI());
                _exps[Groth.G명중] = (12, new GI { { Groth.G연사, 11 } });
                _exps[Groth.G연사] = (12, new GI { { Groth.G명중, 11 } });

                _exps[Groth.G어뢰] = (12, new GI { { Groth.G연사, 11 } });
                _exps[Groth.G대공] = (12, new GI { { Groth.G연사, 11 } });

                _exps[Groth.G수리] = (12, new GI { { Groth.G보수, 11 } });
                _exps[Groth.G보수] = (12, new GI { { Groth.G수리, 11 } });
                _exps[Groth.G기관] = (12, new GI { { Groth.G수리, 11 } });

                _exps[Groth.G함재] = (12, new GI { { Groth.G전투, 10 }, { Groth.G폭격, 10 } });
                _exps[Groth.G전투] = (12, new GI { { Groth.G함재, 10 }, { Groth.G폭격, 10 } });
                _exps[Groth.G폭격] = (12, new GI { { Groth.G함재, 10 }, { Groth.G전투, 10 } });
            }
        }
        #endregion


        #region ---- OCR ----

        public void TestOcr(PictureBox pbColor, PictureBox pbBw)
        {
            WindowHelper.BringMainWindowToFront(_process);
            _origin = WindowHelper.GetWindowPosition(_process);
            _imgStart = ((int)((_origin.X + _groth.X) * _desktopRatio), (int)((_origin.Y + _groth.Y) * _desktopRatio));
            _imgSize = ((int)(20 * _desktopRatio), (int)(150 * _desktopRatio));
            Thread.Sleep(100);

            using var ocr = new Ocr();
            var image = ocr.Capture(_imgStart, _imgSize);
            testOcr(ocr, image, pbColor, pbBw);
        }
        public void TestOcr2(string fileName, PictureBox pbColor, PictureBox pbBw)
        {
            using var ocr = new Ocr();
            var image = new Bitmap(fileName);// ("215822.3_c.png");
            testOcr(ocr, image, pbColor, pbBw);
        }
        void testOcr(Ocr ocr, Bitmap image, PictureBox imgColor, PictureBox imgBw)
        {
            var result = ocr.Process(image, 220);
            imgColor.Image = result.image;
            imgBw.Image = result.imgBw;
            log($"img= {result.image.Size}");

            var text = result.text.Trim().Replace(" ", "").Replace('\n', ' ');
            log($"ocr: {text}");

            var nums = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToArray();
            var sb = new StringBuilder();
            foreach (var n in nums) sb.Append($"{n,03:D2}");
            log(sb);
        }

        #endregion

    }
}
