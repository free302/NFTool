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
    using GV = Dictionary<Groth, int>;
    using GA = Dictionary<Groth, long>;

    internal class AppLogic : IDisposable
    {
        #region ---- UI ----

        Action<object> _logger;
        public AppLogic(Action<object> logger)
        {
            _logger = logger;
            _acts = new GV();

            loadAddress();
            loadNames();
            loadSels();
            initProcess();
        }
        void log(object msg) => _logger?.Invoke(msg);

        public void SaveConfig()
        {
            saveSels();
            saveExps();
            saveNames();
        }
        public void Dispose()
        {
            _procLogic?.Dispose();
        }


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
        Dictionary<Groth, (int priValue, GV secValue)> _exps;
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
                _exps = JsonConvert.DeserializeObject<Dictionary<Groth, (int priValue, GV secValue)>>(text);
            }
            else _exps = new Dictionary<Groth, (int priValue, GV secValue)>();
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


        #region ---- Process ----

        static string _processName = "NavyFIELD";
        ProcessLogic _procLogic;
        GA _address;
        void initProcess()
        {
            _procLogic = new ProcessLogic(_processName);
            log(_procLogic);
        }

        Dictionary<Groth, List<long>> _temp = new Dictionary<Groth, List<long>>();
        void search(GV gis)
        {
            log("Starting search()...");
            foreach (var g in gis.Keys)
            {
                _temp[g] = _procLogic.Search(gis[g]);
                log($"{g}{gis[g],3}{_temp[g].Count,6}");
            }
            log("Quiting search()...");
        }
        void filter(GV gis)
        {
            log("Starting filter()...");
            foreach (var g in gis.Keys)
            {
                f(g, gis[g]);
                log($"{g}{gis[g],3}{_temp[g].Count,6}");
            }
            void f(Groth g, int value) => _temp[g] = _temp[g].Where(a => r(a) == value).ToList();
            int r(long a)
            {
                try { return BitConverter.ToInt32(_procLogic.ReadProcessMemory(a, 4), 0); }
                catch { return 0; }
            }
            log("Quiting filter()...");
        }
        public void testSearch(PictureBox pbColor, PictureBox pbBw)
        {
            //TestOcr(pbColor, pbBw);
            //search(_acts);
            findAddress();
        }
        public void testFilter(PictureBox pbColor, PictureBox pbBw)
        {
            TestOcr(pbColor, pbBw);
            filter(_acts);
        }

        void findAddress()
        {
            log("Starting findAddress()...");
            WindowLogic.WindowToFront(_procLogic.Process);
            Thread.Sleep(1000);
            _imgSize = ((int)(_size.x * _desktopRatio), (int)(_size.y * _desktopRatio));
            _origin = WindowLogic.GetWindowPosition(_procLogic.Process);
            _imgStart = ((int)((_origin.X + _groth.X) * _desktopRatio), (int)((_origin.Y + _groth.Y) * _desktopRatio));

            click(_newButton);
            Thread.Sleep(100);
            _runCounter = 0;

            while (_runCounter++ < 10)
            {
                Thread.Sleep(1000);
                try { read(); }
                catch (Exception ex)
                {
                    log(ex.Message);
                    continue;
                }
                if(_runCounter == 1) search(_acts);
                else filter(_acts);

                var ok = true;
                foreach (var g in _temp.Keys) ok &= (_temp[g].Count <= 2) && (_temp[g].Count > 0);
                if (ok)
                {
                    _sb.Clear();
                    foreach (var g in _temp.Keys)
                    {
                        _address[g] = _temp[g].Last();
                        _sb.Append($"{g}= 0x{_address[g],8:X}\n");
                    }
                    log(_sb.ToString());
                    saveAddress();
                    break;
                }

                click(_renewButton);
            }
            log("Quiting findAddress()...");
        }
        static readonly string _addressFile = "address.json";
        void saveAddress()
        {
            log($"Saving address: {_addressFile}");
            string json = JsonConvert.SerializeObject(_address, Formatting.Indented);
            File.WriteAllText(_addressFile, json);
        }
        void loadAddress()
        {
            if (File.Exists(_addressFile))
            {
                log($"Loading sels: {_addressFile}");
                var text = File.ReadAllText(_addressFile);
                _address = JsonConvert.DeserializeObject<GA>(text);
            }
            else _address = new GA();
        }
        #endregion


        #region ---- Run ----

        static double _desktopRatio = 1.5;
        Ocr _ocr;
        Point _origin;
        Point _newButton = new Point(895, 720);
        Point _renewButton = new Point(665, 420);
        Point _groth = new Point(435, 243);
        (int x, int y) _size = (20, 150);
        int _blackLevel = 200;
        GV _acts;

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
            log("Starting run()...");
            Thread.Sleep(100);
            WindowLogic.WindowToFront(_procLogic.Process);

            _desktopRatio = WindowLogic.GetScalingFactor();
            _imgSize = ((int)(_size.x * _desktopRatio), (int)(_size.y * _desktopRatio));
            _origin = WindowLogic.GetWindowPosition(_procLogic.Process);
            _imgStart = ((int)((_origin.X + _groth.X) * _desktopRatio), (int)((_origin.Y + _groth.Y) * _desktopRatio));

            saveSels();
            saveNames();
            loadExps();
            loadAddress();

            Thread.Sleep(100);
            click(_newButton);
            Thread.Sleep(100);
            _runCounter = 0;

            while (_running)
            {
                _runCounter++;
                WindowLogic.WindowToFront(_procLogic.Process);

                if (!_running) break;
                Thread.Sleep(800);

                try
                {
                    //read();
                    readMemory();
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

            log("Quiting run()...");
            _exitEvent.Set();
        }

        void click(Point clientPoint)
        {
            //Cursor.Position = new Point(_origin.X + clientPoint.X, _origin.Y + clientPoint.Y);
            WindowLogic.SetCursor(_origin.X + clientPoint.X, _origin.Y + clientPoint.Y);
            //log($"cursor= ({Cursor.Position})");
            Thread.Sleep(100);
            SI.SendMouseLeft();

            //SI.SendMouseLeft(_origin.X + clientPoint.X, _origin.Y + clientPoint.Y);
        }

        StringBuilder _sb = new StringBuilder();
        void read()
        {
            using (_ocr = new Ocr())
            {
                var image = Ocr.Capture(_imgStart, _imgSize);
                var result = _ocr.Process(image, _blackLevel);
                var text = result.text.Trim().Replace(" ", "").Replace('\n', ' ');
                //log($"[{_runCounter}] ocr: {text}");

                var nums = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToArray();

                _sb.Clear();
                _sb.Append($"[{_runCounter,5}]");
                foreach (var n in nums) _sb.Append($"{n,03:D2}");
                log(_sb.ToString());
                var isError = nums.Length != 11;
                for (int i = 1; i < nums.Length; i++) isError |= (nums[i] > 12);
                isError |= nums.Length > 0 && nums[0] > 15;
                if (isError)
                {
                    var time = DateTime.Now.ToString("HHmmss.f");
                    result.image.Save($"{time}_c.png", ImageFormat.Png);
                    result.imgBw.Save($"{time}_b.png", ImageFormat.Png);

                    _sb.Clear();
                    _sb.Append($"[{_runCounter,5}] OCR Error({nums.Length}):");
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
                var buffer = _procLogic.ReadProcessMemory(_address[g], 4);
                _acts[g] = BitConverter.ToInt32(buffer, 0);
            }

            _sb.Clear();
            _sb.Append($"[{_runCounter,5}]");
            foreach (var g in _acts.Keys) _sb.Append($"{_acts[g],03:D2}");
            log(_sb.ToString());
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
            //readMemory();
            //return;

            //saveSels();
            //saveNames();

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
                _exps = new Dictionary<Groth, (int priValue, GV secValue)>();
                _exps[Groth.G잠재] = (15, new GV());
                _exps[Groth.G명중] = (12, new GV { { Groth.G연사, 11 } });
                _exps[Groth.G연사] = (12, new GV { { Groth.G명중, 11 } });

                _exps[Groth.G어뢰] = (12, new GV { { Groth.G연사, 11 } });
                _exps[Groth.G대공] = (12, new GV { { Groth.G연사, 11 } });

                _exps[Groth.G수리] = (12, new GV { { Groth.G보수, 11 } });
                _exps[Groth.G보수] = (12, new GV { { Groth.G수리, 11 } });
                _exps[Groth.G기관] = (12, new GV { { Groth.G수리, 11 } });

                _exps[Groth.G함재] = (12, new GV { { Groth.G전투, 10 }, { Groth.G폭격, 10 } });
                _exps[Groth.G전투] = (12, new GV { { Groth.G함재, 10 }, { Groth.G폭격, 10 } });
                _exps[Groth.G폭격] = (12, new GV { { Groth.G함재, 10 }, { Groth.G전투, 10 } });
            }
        }
        #endregion


        #region ---- OCR ----

        public void SetOrigin()
        {
            WindowLogic.SetWindowPosition(_procLogic.Process, 1535, 671);
        }

        public void TestOcr(PictureBox pbColor, PictureBox pbBw)
        {
            //--- test ----
            //_processName = "WindowsFormsApp1";//"NavyFIELD";
            //_newButton = new Point(100, 100);
            //_renewButton = new Point(100, 100);
            //_groth = new Point(19, 32);
            //_size = (23, 260);
            //_blackLevel = 180;

            WindowLogic.WindowToFront(_procLogic.Process);
            _desktopRatio = WindowLogic.GetScalingFactor();
            log($"_desktopRatio= {_desktopRatio:N2}");

            _origin = WindowLogic.GetWindowPosition(_procLogic.Process);
            _imgStart = ((int)((_origin.X + _groth.X) * _desktopRatio), (int)((_origin.Y + _groth.Y) * _desktopRatio));
            _imgSize = ((int)(_size.x * _desktopRatio), (int)(_size.y * _desktopRatio));
            Thread.Sleep(500);

            click(_renewButton);
            Thread.Sleep(1000);

            using var ocr = new Ocr();
            var image = Ocr.Capture(_imgStart, _imgSize);
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
            var result = ocr.Process(image, _blackLevel);
            imgColor.Image = result.image;
            imgBw.Image = result.imgBw;
            //log($"img= {result.image.Size} => {result.imgBw.Size}");

            var text = result.text.Trim().Replace(" ", "").Replace('\n', ' ');
            //log($"ocr: {text}");

            var nums = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToArray();

            var isError = nums.Length != 11;
            for (int i = 1; i < nums.Length; i++) isError |= (nums[i] > 12);
            isError |= nums.Length > 0 && nums[0] > 15;

            _sb.Clear();
            _sb.Append($"[{_runCounter,5}] OCR {(isError?"ERROR":"OK")}({nums.Length}):");
            foreach (var n in nums) _sb.Append($"{n,03:D2}");
            log(_sb.ToString());

            var time = DateTime.Now.ToString("HHmmss.f");
            result.image.Save($"{time}_c.png", ImageFormat.Png);
            result.imgBw.Save($"{time}_b.png", ImageFormat.Png);

            if(!isError) for (int i = 0; i < nums.Length; i++) _acts[(Groth)i] = nums[i];
        }

        #endregion

    }
}
