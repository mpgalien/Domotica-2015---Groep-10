﻿// Xamarin/C# app voor de besturing van een Arduino (Uno with Ethernet Shield) m.b.v. een socket-interface.
// Arduino server: DomoticaServer.ino
// De besturing heeft betrekking op het aan- en uitschakelen van een Arduino pin, 
// waar een led aan kan hangen of, t.b.v. het Domotica project, een RF-zender waarmee een 
// klik-aan-klik-uit apparaat bestuurd kan worden.
//
// De app heeft twee modes die betrekking hebben op de afhandeling van de socket-communicatie: "simple-mode" en "threaded-mode" 
// Wanneer het statement    //connector = new Connector(this);    wordt uitgecommentarieerd draait de app in "simple-mode",
// Het opvragen van gegevens van de Arduino (server) wordt dan met een Timer gerealisseerd. (De extra classes Connector.cs, 
// Receiver.cs en Sender.cs worden dan niet gebruikt.) 
// Als er een connector wordt aangemaakt draait de app in "threaded mode". De socket-communicatie wordt dan afgehandeld
// via een Sender- en een Receiver klasse, die worden aangemaakt in de Connector klasse. Deze threaded mode 
// biedt een generiekere en ook robuustere manier van communicatie, maar is ook moeilijker te begrijpen. 
// Aanbeveling: start in ieder geval met de simple-mode
//
// Werking: De communicatie met de (Arduino) server is gebaseerd op een socket-interface. Het IP- en Port-nummer
// is instelbaar. Na verbinding kunnen, middels een eenvoudig commando-protocol, opdrachten gegeven worden aan 
// de server (bijv. pin aan/uit). Indien de server om een response wordt gevraagd (bijv. led-status of een
// sensorwaarde), wordt deze in een 4-bytes ASCII-buffer ontvangen, en op het scherm geplaatst. Alle commando's naar 
// de server zijn gecodeerd met 1 char. Bestudeer het protocol in samenhang met de code van de Arduino server.
// Het default IP- en Port-nummer (zoals dat in het GUI verschijnt) kan aangepast worden in de file "Strings.xml". De
// ingestelde waarde is gebaseerd op je eigen netwerkomgeving, hier, en in de Arduino-code, is dat een router, die via DHCP
// in het segment 192.168.1.x vanaf systeemnummer 100 IP-adressen uitgeeft.
// 
// Resource files:
//   Main.axml (voor het grafisch design, in de map Resources->layout)
//   Strings.xml (voor alle statische strings in het interface, in de map Resources->values)
// 
// De software is verder gedocumenteerd in de code. Tijdens de colleges wordt er nadere uitleg over gegeven.
// 
// Versie 1.0, 12/12/2015
// D. Bruin
// S. Oosterhaven
// W. Dalof (voor de basis van het Threaded interface)
//
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content.PM;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Android.Graphics;
using System.Threading.Tasks;

namespace Domotica
{
    [Activity(Label = "@string/application_name", MainLauncher = true, Icon = "@drawable/icon", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait)]

    public class MainActivity : Activity
    {
        // Variables (components/controls)
        // Controls on GUI
        Button buttonConnect;
        Button buttonChangePinState;
		Button buttonChangePinState2;
		Button buttonChangePinState3;
		Button smtemp;
		Button smlicht;
		Button tempSwitch;
		Button lightSwitch;
		Button count;
		Button buttonC;
		Button buttonC_1;
        TextView textViewServerConnect;
		public TextView textViewChangePinStateValue, textViewChangePinStateValue2, textViewChangePinStateValue3,  textViewSensorValue, textViewSensorValueb, textViewDebugValue, textviewSeconds;
		EditText editTextIPAddress, editTextIPPort, tempvalue, lichtvalue, countvalue;
		bool sensor = false;
		bool tempsensor = false;
		bool lightsensor = false;
		bool specialtemp = false;
		bool speciallicht = false;
		bool tempvoltageoff = true;
		bool lichtvoltageoff = true;
		bool tempfirst = false;
		bool lightfirst = false;
		int lichtvalue1;
		int tempvalue1;
		int teller = 0;
		int timerCounter;

        Timer timerSockets, timerCount;             // Timers   
        Socket socket = null;                       // Socket   
        Connector connector = null;                 // Connector (simple-mode or threaded-mode)
        List<Tuple<string, TextView>> commandList = new List<Tuple<string, TextView>>();  // List for commands and response places on UI
        int listIndex = 0;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource (strings are loaded from Recources -> values -> Strings.xml)
            SetContentView(Resource.Layout.Main);

            // find and set the controls, so it can be used in the code
            buttonConnect = FindViewById<Button>(Resource.Id.buttonConnect);
			buttonC = FindViewById<Button>(Resource.Id.buttonC);
			buttonC_1 = FindViewById<Button> (Resource.Id.buttonC_1);
            buttonChangePinState = FindViewById<Button>(Resource.Id.buttonChangePinState);
			buttonChangePinState2 = FindViewById<Button> (Resource.Id.buttonChangePinState2);
			buttonChangePinState3 = FindViewById<Button> (Resource.Id.buttonChangePinState3);
			tempSwitch = FindViewById<Button> (Resource.Id.tempSwitch);
			lightSwitch = FindViewById<Button> (Resource.Id.lightSwitch);
			smtemp = FindViewById<Button> (Resource.Id.smtemp);
			smlicht = FindViewById<Button> (Resource.Id.smlicht);
            textViewServerConnect = FindViewById<TextView>(Resource.Id.textViewServerConnect);
            textViewChangePinStateValue = FindViewById<TextView>(Resource.Id.textViewChangePinStateValue);
			textViewChangePinStateValue2 = FindViewById<TextView>(Resource.Id.textViewChangePinStateValue2);
			textViewChangePinStateValue3 = FindViewById<TextView>(Resource.Id.textViewChangePinStateValue3);
            textViewSensorValue = FindViewById<TextView>(Resource.Id.textViewSensorValue);
			textViewSensorValueb = FindViewById<TextView>(Resource.Id.textViewSensorValueb);
            editTextIPAddress = FindViewById<EditText>(Resource.Id.editTextIPAddress);
            editTextIPPort = FindViewById<EditText>(Resource.Id.editTextIPPort);
			tempvalue = FindViewById<EditText>(Resource.Id.tempvalue);
			lichtvalue = FindViewById<EditText>(Resource.Id.lichtvalue);
			count = FindViewById<Button> (Resource.Id.count);
			countvalue = FindViewById<EditText>(Resource.Id.countvalue);
			textviewSeconds = FindViewById<TextView>(Resource.Id.textViewSeconds);


            UpdateConnectionState(4, "Disconnected"); 

            // Init commandlist, scheduled by socket timer
            commandList.Add(new Tuple<string, TextView>("s", textViewChangePinStateValue));
			commandList.Add(new Tuple<string, TextView>("t", textViewChangePinStateValue2));
			commandList.Add(new Tuple<string, TextView>("u", textViewChangePinStateValue3));

            // activation of connector -> threaded sockets otherwise -> simple sockets 
            // connector = new Connector(this);

            this.Title = (connector == null) ? this.Title + " (simple sockets)" : this.Title + " (thread sockets)";


			// timer object, running clock
			timerCount = new System.Timers.Timer() { Interval = 1000, Enabled = false }; // Interval >= 1000
			timerCount.Elapsed += (obj, args) =>
			{
				teller++;
				if(teller == timerCounter)
				{
					socket.Send(Encoding.ASCII.GetBytes("x"));
					teller = 0;
					timerCount.Enabled = false;
				}
			};

            // timer object, check Arduino state
            // Only one command can be serviced in an timer tick, schedule from list
            timerSockets = new System.Timers.Timer() { Interval = 1000, Enabled = false }; // Interval >= 750
            timerSockets.Elapsed += (obj, args) =>
            {
                //RunOnUiThread(() =>
                //{
                    if (socket != null) // only if socket exists
                    {
                        // Send a command to the Arduino server on every tick (loop though list)
                        UpdateGUI(executeCommand(commandList[listIndex].Item1), commandList[listIndex].Item2);  //e.g. UpdateGUI(executeCommand("s"), textViewChangePinStateValue);
                        if (++listIndex >= commandList.Count) listIndex = 0;
                    }
                    else timerSockets.Enabled = false;  // If socket broken -> disable timer

				if(tempsensor || lightsensor)
				{
					if(specialtemp)
					{
						tempvalue1 = Convert.ToInt16(tempvalue.Text);
						if(tempvalue1 < Convert.ToInt16(textViewSensorValue.Text) && tempvoltageoff)
						{
							socket.Send(Encoding.ASCII.GetBytes("x"));
							tempvoltageoff = false;
						}
						else if(tempvalue1 > Convert.ToInt16(textViewSensorValue.Text) && !tempvoltageoff)
						{
							socket.Send(Encoding.ASCII.GetBytes("x"));
							tempvoltageoff = true;
						}
					}

					if(speciallicht)
					{
						lichtvalue1 = Convert.ToInt16(lichtvalue.Text);
						if(lichtvalue1 > Convert.ToInt16(textViewSensorValueb.Text) && lichtvoltageoff)
						{
							socket.Send(Encoding.ASCII.GetBytes("y"));
							lichtvoltageoff = false;
						}
						else if(lichtvalue1 < Convert.ToInt16(textViewSensorValueb.Text) && !lichtvoltageoff)
						{
							socket.Send(Encoding.ASCII.GetBytes("y"));
							lichtvoltageoff = true;
						}
					}

				}

                //});
            };

			//string ip = "192.168.1.104";
			string port = "3300";
			string [] ip = new string [1];
			ip [0] = "192.168.1.5";


			int i = 0;
			while (socket == null) 
			{
				ConnectSocket (ip[i], port); //Direct connect to arduino
				i++;
			}


            //Add the "Connect" button handler.
            if (buttonConnect != null)  // if button exists
            {
                buttonConnect.Click += (sender, e) =>
                {
                    //Validate the user input (IP address and port)
                    if (CheckValidIpAddress(editTextIPAddress.Text) && CheckValidPort(editTextIPPort.Text))
                    {
                        if (connector == null) // -> simple sockets
                        {
							ConnectSocket(ip[0], port);
                        }
                        else // -> threaded sockets
                        {
                            //Stop the thread If the Connector thread is already started.
                            if (connector.CheckStarted()) connector.StopConnector();
                               connector.StartConnector(editTextIPAddress.Text, editTextIPPort.Text);
                        }
                    }
                    else UpdateConnectionState(3, "Please check IP");
                };
            }

			//Add the "Connect" button handler.
			if (buttonC != null)  // if button exists
			{
				buttonC.Click += (sender, e) =>
				{
					socket.Send(Encoding.ASCII.GetBytes("g"));
				};
			}

			//Add the "Connect" button handler.
			if (buttonC_1 != null)  // if button exists
			{
				buttonC_1.Click += (sender, e) =>
				{
					socket.Send(Encoding.ASCII.GetBytes("h"));
				};
			}

			if (count != null)  // if button exists
			{
				count.Click += (sender, e) =>
				{
					timerCounter = Convert.ToInt16(countvalue.Text);
					timerCount.Enabled = true;
				};
			}

			if (tempSwitch != null)  // if button exists
			{
				tempSwitch.Click += (sender, e) =>
				{
					if(tempsensor == false)
					{
						if(lightfirst)
						{
							tempfirst = false;
						}
						else
						{
							tempfirst = true;
						}
						tempsensor = true;
						commandList.Add(new Tuple<string, TextView>("a", textViewSensorValue));
					}
					else
					{
						tempsensor = false;
						if(lightsensor)
						{
							if(lightfirst)
							{
								commandList.RemoveAt(4);
							}
							else
							{
								commandList.RemoveAt(3);
							}
						}
						else
						{
							commandList.RemoveAt(3);
						}
						textViewSensorValue.Text = "-";
					}
				};
			}

			if (lightSwitch != null)  // if button exists
			{
				lightSwitch.Click += (sender, e) =>
				{
					if(lightsensor == false)
					{
						if(tempfirst)
						{
							lightfirst = false;
						}
						else
						{
							lightfirst = true;
						}
						lightsensor = true;
						commandList.Add(new Tuple<string, TextView>("b", textViewSensorValueb));
					}
					else
					{
						lightsensor = false;
						if(tempsensor)
						{
							if(tempfirst)
							{
								commandList.RemoveAt(4);
							}
							else
							{
								commandList.RemoveAt(3);
							}
						}
						else
						{
							commandList.RemoveAt(3);
						}
						textViewSensorValueb.Text = "-";
					}
				};
			}

			if (smtemp != null)  // if button exists
			{
				smtemp.Click += (sender, e) =>
				{
					if(specialtemp == false)
					{
						specialtemp = true;
					}
					else
					{
						specialtemp = false;
						if(!tempvoltageoff)
						{
							socket.Send(Encoding.ASCII.GetBytes("x"));
							tempvoltageoff = true;
						}
					}
				};
			}

			if (smlicht != null)  // if button exists
			{
				smlicht.Click += (sender, e) =>
				{
					if(speciallicht == false)
					{
						speciallicht = true;
					}
					else
					{
						speciallicht = false;
						if(!lichtvoltageoff)
						{
							socket.Send(Encoding.ASCII.GetBytes("y"));
							lichtvoltageoff = true;
						}
					}
				};
			}

            //Add the "Change pin state" button handler.
            if (buttonChangePinState != null)
            {
                buttonChangePinState.Click += (sender, e) =>
                {
                    if (connector == null) // -> simple sockets
                    {
                        socket.Send(Encoding.ASCII.GetBytes("x"));                 // Send toggle-command to the Arduino
                    }
                    else // -> threaded sockets
                    {
                        if (connector.CheckStarted()) connector.SendMessage("x");  // Send toggle-command to the Arduino
                    }
                };
            }

			if (buttonChangePinState2 != null)
			{
				buttonChangePinState2.Click += (sender, e) =>
				{
					if (connector == null) // -> simple sockets
					{
						socket.Send(Encoding.ASCII.GetBytes("y"));                 // Send toggle-command to the Arduino
					}
					else // -> threaded sockets
					{
						if (connector.CheckStarted()) connector.SendMessage("y");  // Send toggle-command to the Arduino
					}
				};
			}

			if (buttonChangePinState3 != null)
			{
				buttonChangePinState3.Click += (sender, e) =>
				{
					if (connector == null) // -> simple sockets
					{
						socket.Send(Encoding.ASCII.GetBytes("z"));                 // Send toggle-command to the Arduino
					}
					else // -> threaded sockets
					{
						if (connector.CheckStarted()) connector.SendMessage("z");  // Send toggle-command to the Arduino
					}
				};
			}
        }


        //Send command to server and wait for response (blocking)
        //Method should only be called when socket existst
        public string executeCommand(string cmd)
        {
            byte[] buffer = new byte[4]; // response is always 4 bytes
            int bytesRead = 0;
            string result = "---";

            if (socket != null)
            {
                //Send command to server
                socket.Send(Encoding.ASCII.GetBytes(cmd));

                try //Get response from server
                {
                    //Store received bytes (always 4 bytes, ends with \n)
                    bytesRead = socket.Receive(buffer);  // If no data is available for reading, the Receive method will block until data is available,
                    //Read available bytes.              // socket.Available gets the amount of data that has been received from the network and is available to be read
                    while (socket.Available > 0) bytesRead = socket.Receive(buffer);
                    if (bytesRead == 4)
                        result = Encoding.ASCII.GetString(buffer, 0, bytesRead - 1); // skip \n
                    else result = "err";
                }
                catch (Exception exception) {
                    result = exception.ToString();
                    if (socket != null) {
                        socket.Close();
                        socket = null;
                    }
                    UpdateConnectionState(3, result);
                }
            }
            return result;
        }

        //Update connection state label (GUI).
        public void UpdateConnectionState(int state, string text)
        {
            // connectButton
            string butConText = "Connect";  // default text
            bool butConEnabled = true;      // default state
            Color color = Color.Red;        // default color
            // pinButton
            bool butPinEnabled = false;     // default state 

            //Set "Connect" button label according to connection state.
            if (state == 1)
            {
                butConText = "Please wait";
                color = Color.Orange;
                butConEnabled = false;
            } else
            if (state == 2)
            {
                butConText = "Disconnect";
                color = Color.Green;
                butPinEnabled = true;
            }
            //Edit the control's properties on the UI thread
            RunOnUiThread(() =>
            {
                textViewServerConnect.Text = text;
                if (butConText != null)  // text existst
                {
                    buttonConnect.Text = butConText;
                    textViewServerConnect.SetTextColor(color);
                    buttonConnect.Enabled = butConEnabled;
                }
                	buttonChangePinState.Enabled = butPinEnabled;
					buttonChangePinState2.Enabled = butPinEnabled;
					buttonChangePinState3.Enabled = butPinEnabled;
					tempSwitch.Enabled = butPinEnabled;
					lightSwitch.Enabled = butPinEnabled;
					smtemp.Enabled = butPinEnabled;
					smlicht.Enabled = butPinEnabled;
					count.Enabled = butPinEnabled;
            });
        }

        //Update GUI based on Arduino response
        public void UpdateGUI(string result, TextView textview)
        {
            RunOnUiThread(() =>
            {
                if (result == "OFF") textview.SetTextColor(Color.Red);
                else if (result == " ON") textview.SetTextColor(Color.Green);
                else textview.SetTextColor(Color.White);  
                textview.Text = result;
            });
        }

        // Connect to socket ip/prt (simple sockets)
        public void ConnectSocket(string ip, string prt)
        {
                if (socket == null)                                       // create new socket
                {
                    UpdateConnectionState(1, "Connecting...");
                    try  // to connect to the server (Arduino).
                    {
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socket.Connect(new IPEndPoint(IPAddress.Parse(ip), Convert.ToInt32(prt)));
                        if (socket.Connected)
                        {
                            UpdateConnectionState(2, "Connected");
                            timerSockets.Enabled = true;                //Activate timer for communication with Arduino     
                        }
                    } catch (Exception exception) {
                        timerSockets.Enabled = false;
                        if (socket != null)
                        {
                            socket.Close();
                            socket = null;
                        }
                        UpdateConnectionState(4, exception.Message);
                    }
	            }
                else // disconnect socket
                {
                    socket.Close(); socket = null;
                    timerSockets.Enabled = false;
                    UpdateConnectionState(4, "Disconnected");
                }
        }

        //Close the connection (stop the threads) if the application stops.
        protected override void OnStop()
        {
            base.OnStop();

            if (connector != null)
            {
                if (connector.CheckStarted())
                {
                    connector.StopConnector();
                }
            }
        }

        //Close the connection (stop the threads) if the application is destroyed.
        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (connector != null)
            {
                if (connector.CheckStarted())
                {
                    connector.StopConnector();
                }
            }
        }

        //Prepare the Screen's standard options menu to be displayed.
        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            //Prevent menu items from being duplicated.
            menu.Clear();

            MenuInflater.Inflate(Resource.Menu.menu, menu);
            return base.OnPrepareOptionsMenu(menu);
        }

        //Executes an action when a menu button is pressed.
        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.exit:
                    //Force quit the application.
                    System.Environment.Exit(0);
                    return true;
                case Resource.Id.abort:

                    //Stop threads forcibly (for debugging only).
                    if (connector != null)
                    {
                        if (connector.CheckStarted()) connector.Abort();
                    }
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        //Check if the entered IP address is valid.
        private bool CheckValidIpAddress(string ip)
        {
            if (ip != "") {
                //Check user input against regex (check if IP address is not empty).
                Regex regex = new Regex("\\b((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)(\\.|$)){4}\\b");
                Match match = regex.Match(ip);
                return match.Success;
            } else return false;
        }

        //Check if the entered port is valid.
        private bool CheckValidPort(string port)
        {
            //Check if a value is entered.
            if (port != "")
            {
                Regex regex = new Regex("[0-9]+");
                Match match = regex.Match(port);

                if (match.Success)
                {
                    int portAsInteger = Int32.Parse(port);
                    //Check if port is in range.
                    return ((portAsInteger >= 0) && (portAsInteger <= 65535));
                }
                else return false;
            } else return false;
        }
    }
}
