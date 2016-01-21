#include <SPI.h>                  // Ethernet shield uses SPI-interface
#include <Ethernet2.h>             // Ethernet library
#include <RCSwitch.h>         // Remote Control (Action, new model)

RCSwitch mySwitch = RCSwitch();

//Set Ethernet Shield MAC address  (check yours)
byte mac[] = { 0x40, 0x6c, 0x8f, 0x36, 0x84, 0x8a };
IPAddress ip(192, 168, 1, 3);
int ethPort = 3300;

#define lowPin       5  // output, always LOW
#define highPin      6  // output, always HIGH
#define switchPin    4  // input, connected to some kind of inputswitch
#define ledPin1       7  // output, led used for "connect state": blinking = searching; continuously = connected
#define ledPin2       8
#define ledPin3       9
#define infoPin      3  // output, more information
#define analogPin    0  // sensor value
#define analogPin1    1  // sensor value

EthernetServer server(ethPort);              // EthernetServer instance (listening on port <ethPort>).

bool pinState1 = false;                   // Variable to store actual pin state
bool pinState2 = false;  
bool pinState3 = false;  
bool pinChange1 = false;                  // Variable to store actual pin change
bool pinChange2 = false;
bool pinChange3 = false;
float  sensorValue = 0;   
int  sensorValue1 = 0;
double val;

void setup()
{
  //Init I/O-pins
   pinMode(switchPin, INPUT);            // hardware switch, for changing pin state
   pinMode(lowPin, OUTPUT);
   pinMode(highPin, OUTPUT);
   pinMode(ledPin1, OUTPUT);
   pinMode(ledPin2, OUTPUT);
   pinMode(ledPin3, OUTPUT);
   pinMode(infoPin, OUTPUT);

   //Default states
   digitalWrite(switchPin, HIGH);        // Activate pullup resistors (needed for input pin)
   digitalWrite(lowPin, LOW);
   digitalWrite(highPin, HIGH);
   //digitalWrite(RFPin, LOW);
   digitalWrite(ledPin1, LOW);
   digitalWrite(ledPin2, LOW);
   digitalWrite(ledPin3, LOW);
   digitalWrite(infoPin, LOW);
   

   mySwitch.enableTransmit(6);

   Serial.begin(9600);

  //Try to get an IP address from the DHCP server.
  if (Ethernet.begin(mac) == 0)
  {
     Serial.println("Could not obtain IP-address from DHCP -> do nothing");
      while (true){     // no point in carrying on, so do nothing forevermore; check your router
      }
  }

   Serial.println("Connection State: "); Serial.print(ledPin1);
   Serial.println("Input switch on pin "); Serial.print(switchPin);
   
  //Start the ethernet server.
    server.begin();

  // Print IP-address and led indication of server state
  Serial.print("Listening address: ");
  Serial.print(Ethernet.localIP());
}

void loop()
{
   // Listen for incomming connection (app)
  EthernetClient ethernetClient = server.available();
  if (!ethernetClient) {
      blink(ledPin1);
     return; // wait for connection and blink LED
  }

  Serial.println("Application connected");
   digitalWrite(ledPin1, LOW);
// Do what needs to be done while the socket is connected.
  while (ethernetClient.connected()) 
  {
      checkEvent(switchPin, pinState1);
      val = analogRead(0)*5/1024.0;
      val = val - 0.5;
      val = val / 0.01;
      sensorValue = val / 3.2;
      sensorValue1 = analogRead(1);
        
     // Activate pin based op pinState
     if (pinChange1) {
        if (pinState1) { digitalWrite(ledPin1, HIGH); }
         else { digitalWrite(ledPin1, LOW); }
         pinChange1 = false;
         delay(100); // delay depends on device
     }
     if (pinChange2) {
        if (pinState2) { digitalWrite(ledPin2, HIGH); }
         else { digitalWrite(ledPin2, LOW); }
         pinChange2 = false;
         delay(100); // delay depends on device
     }
     if (pinChange3) {
        if (pinState3) { digitalWrite(ledPin3, HIGH); }
         else { digitalWrite(ledPin3, LOW); }
         pinChange3 = false;
         delay(100); // delay depends on device
     }
   
     // Execute when byte is received.
     while (ethernetClient.available())
    {
       char inByte = ethernetClient.read();   // Get byte from the client.
       executeCommand(inByte);                // Wait for command to execute
        inByte = NULL;                         // Reset the read byte.
     } 
   }
  Serial.println("Application disonnected");
}

// Response (to app) is 4 chars  (not all commands demand a response)
void executeCommand(char cmd)
{     
         char buf[4] = {'\0', '\0', '\0', '\0'};

         // Command protocol
         Serial.print("["); Serial.print(cmd); Serial.print("] -> ");
         switch (cmd) {
         case 'a': // Report sensor value to the app  
            intToCharBuf(sensorValue, buf, 4);                // convert to charbuffer
            server.write(buf, 4);                             // response is always 4 chars (\n included)
            Serial.print("Sensor Tem: "); Serial.println(buf);
            break;
         case 'b': // Report sensor value to the app  
            intToCharBuf(sensorValue1, buf, 4);                // convert to charbuffer
            server.write(buf, 4);                             // response is always 4 chars (\n included)
            Serial.print("Sensor Light: "); Serial.println(buf);
            break;
         case 's': // Report switch state to the app
            if (pinState1) { server.write(" ON\n"); Serial.println("Pin state is ON"); }  // always send 4 chars
            else { server.write("OFF\n"); Serial.println("Pin state is OFF"); }
            break;
         case 't': // Report switch state to the app
            if (pinState2) { server.write(" ON\n"); Serial.println("Pin state is ON"); }  // always send 4 chars
            else { server.write("OFF\n"); Serial.println("Pin state is OFF"); }
            break;
         case 'u': // Report switch state to the app
            if (pinState3) { server.write(" ON\n"); Serial.println("Pin state is ON"); }  // always send 4 chars
            else { server.write("OFF\n"); Serial.println("Pin state is OFF"); }
            break;   
         case 'x': // Toggle state; If state is already ON then turn it OFF
            if (pinState1) { pinState1 = false; Serial.println("Set pin state to \"OFF\""); 
             mySwitch.send("000000111010001101101110"); } 
             //willem mySwitch.send("001001110111001000101110"); } 
            else { pinState1 = true; Serial.println("Set pin state to \"ON\""); 
              mySwitch.send("000000111010001101101111");
              //willem mySwitch.send("001001110111001000101111");
            }  
            pinChange1 = true; 
            break;
            case 'y': // Toggle state; If state is already ON then turn it OFF
            if (pinState2) { pinState2 = false; Serial.println("Set pin state to \"OFF\""); 
             //willem: mySwitch.send("001001110111001000101100");}
             //mySwitch.send("010110100100010000101100");}
             mySwitch.send("000000111010001101101100");}
            else { pinState2 = true; Serial.println("Set pin state to \"ON\"");
              mySwitch.send("000000111010001101101101");
              //mySwitch.send("010110100100010000101101"); 
              //willem: mySwitch.send("001001110111001000101101"); 
            }  
            pinChange2 = true; 
            break;
            case 'z': // Toggle state; If state is already ON then turn it OFF
if (pinState3) { pinState3 = false; Serial.println("Set pin state to \"OFF\"");
            mySwitch.send("000000111010001101101010");} 
             //willem: mySwitch.send("010110100100010000101010");}
            else { pinState3 = true; Serial.println("Set pin state to \"ON\""); 
              //willem: mySwitch.send("010110100100010000101011");
              mySwitch.send("000000111010001101101011"); 
            }  
            pinChange3 = true; 
            break;
         case 'i':    
            digitalWrite(infoPin, HIGH);
            break;
         default:
            digitalWrite(infoPin, LOW);
         }
}

// read value from pin pn, return value is mapped between 0 and mx-1
int readSensor(int pn, int mx)
{
  return map(analogRead(pn), 0, 100, 0, mx-1);    
}

// Convert int <val> char buffer with length <len>
void intToCharBuf(int val, char buf[], int len)
{
   String s;
   s = String(val);                        // convert tot string
   if (s.length() == 1) s = "0" + s;       // prefix redundant "0" 
   if (s.length() == 2) s = "0" + s;  
   s = s + "\n";                           // add newline
   s.toCharArray(buf, len);                // convert string to char-buffer
}

// Check switch level and determine if an event has happend
// event: low -> high or high -> low
void checkEvent(int p, bool &state)
{
   static bool swLevel = false;       // Variable to store the switch level (Low or High)
   static bool prevswLevel = false;   // Variable to store the previous switch level

   swLevel = digitalRead(p);
   if (swLevel)
      if (prevswLevel) delay(1);
      else {               
         prevswLevel = true;   // Low -> High transition
         state = true;
         pinChange1 = true;
      } 
   else // swLevel == Low
      if (!prevswLevel) delay(1);
      else {
         prevswLevel = false;  // High -> Low transition
         state = false;
         pinChange1 = true;
      }
}

// blink led on pin <pn>
void blink(int pn)
{
  digitalWrite(pn, HIGH); delay(400); digitalWrite(pn, LOW); delay(400);
}

// Returns B-class network-id: 192.168.1.105 -> 1)
int getIPClassB(IPAddress address)
{
    return address[2];
}
