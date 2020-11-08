using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NFT.NavyReader.ref1
{
    using KL = List<(Keys key, IntPtr wp)>;
    using IL = List<INPUT>;
    class KeysPlayer
    {
        private KL _keysToPlay; // Keys to play, with the timing. See KeysSaver.savedKeys for more informations.
        private IL _playedKeys; // The inputs that will be played. This is a "translation" of keysToPlay, transforming Keys into Inputs.
        private Stopwatch watch; // Timer used to respect the strokes timing.
        public KeysPlayer(KL keysToPlay)
        {
            _keysToPlay = keysToPlay;
            _playedKeys = new IL();
            watch = new Stopwatch();
            loadPlayedKeys(); //Load the keys that will be played.
        }
        public void Start()
        {
            watch.Restart(); //Resets the timer            
            var inputs = new INPUT[1];
            Debug.WriteLine($"num keys = {_playedKeys.Count}");
            foreach(var i in _playedKeys)
            {
                inputs[0] = i;
                uint err = SendInput(1, inputs, INPUT.Size); //Simulate the inputs of the actual frame
            }
        }
        public void Stop() => watch.Stop(); //Stops the timer.

        /*
         * method loadPlayedKeys()
         * Description : Transforms the keysToPlay dictionnary into a sequence of inputs. Also, pre-load the inputs we need (loading takes a bit of time that could lead to desyncs).
         */
        private void loadPlayedKeys()
        {
            _playedKeys.Clear();
            foreach (var t in _keysToPlay) _playedKeys.Add(loadKey(t.key, intPtrToFlags(t.wp))); //Load the key that will be played and adds it to the list. 
        }

        //Description : Translate the IntPtr which references the activity (keydown/keyup) into input flags.
        private UInt32 intPtrToFlags(IntPtr activity) => (activity == KeysSaver.KEYUP) ? 0x0002u : 0x0000;

        /*
         * method loadKey()
         * Description : Transforms the Key into a sendable input (using the above structures).
         */
        private INPUT loadKey(Keys key, UInt32 flags)
        {
            return new INPUT
            {
                Type = 1, //1 = "this is a keyboad event"
                Data =
                {
                    Keyboard = new KEYBDINPUT
                    {
                        KeyCode = (UInt16)key,
                        Scan = 0,
                        Flags = flags,
                        Time = 0,
                        ExtraInfo = IntPtr.Zero
                    }
                }

            };
        }

        // Importation of native libraries
        [DllImport("user32.dll", SetLastError = true)]
        public static extern UInt32 SendInput(UInt32 numberOfInputs, INPUT[] inputs, Int32 sizeOfInputStructure);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

    }
}
