/* NHL Teacher Watcher
 * Groep 10
 * Domotica Project
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Locations;
using Android.OS;
using Android.Util;
using Android.Widget;
using System.Data;
using System.Timers;
using System.Net;
using System.Collections.Specialized;
using Android.Graphics;
using Android.Content;

namespace com.xamarin.recipes.teacherwatcher
{
	[Activity(Label = "@string/application_name", MainLauncher = true, Icon = "@drawable/ic_launcher")]
    public class Activity1 : Activity, ILocationListener
    {
        Location _currentLocation;
        LocationManager _locationManager;
		Timer timerCount;
		Button login;
		TextView checktest;
		TextView presence;

        string _locationProvider;
		EditText editEmail;
		ToggleButton toggleUpdate;
		bool update = false;
		string adres = null;
		bool wrongemail;
		bool loggedIn = false;
		List<string> mailadressen = new List<string> {"willem-de-jong@hotmail.com", "mpgalien@gmail.com", "j.foppele@nhl.nl"};


		//Standaard functie Android, zodra de Locatie veranderd wordt deze opgeslagen in een variable
        public void OnLocationChanged(Location location)
        {
			_currentLocation = location;
			if (timerCount.Enabled == false && adres != null) {
				timerCount.Enabled = true;
			}

        }
		//Standaard functie Android, voor locatie!
        public void OnProviderDisabled(string provider)
        {
        }
		//Standaard functie Android, voor locatie!
        public void OnProviderEnabled(string provider)
        {
        }
		//Standaard functie Android, voor locatie!
        public void OnStatusChanged(string provider, Availability status, Bundle extras)
        {
        }

		//Functie die wordt aangeroepen wanneer de app wordt opgestart
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

			ImageView imageView = FindViewById<ImageView> (Resource.Id.demoImageView);
			editEmail = FindViewById<EditText> (Resource.Id.editEmail);
			login = FindViewById<Button> (Resource.Id.login);
			checktest = FindViewById<TextView> (Resource.Id.checktest);
			presence = FindViewById<TextView>(Resource.Id.presence);

			toggleUpdate = FindViewById<ToggleButton> (Resource.Id.toggleUpdate);

			InitializeLocationManager();

			if (!loggedIn) {
				checktest.SetTextColor (Color.DarkGray);
				presence.SetTextColor (Color.DarkGray);
				toggleUpdate.Enabled = false;
			}

			retrieveset ();

			/*All timerCount is ingeschakeld wordt op basis van de locatiegegevens die de telefoon doorgeeft bepaalt of je binnen de grenzen
			 * van de NHL bent. Als dit het geval is wordt aangegeven in de app doormiddel van een groene tekst en een oplichtend plaatje van
			 * het NHL gebouw. Vervolgens worden er een aantal gegevens verstuurd naar de webserver.
			 */
			timerCount = new System.Timers.Timer() { Interval = 2000, Enabled = false };
			timerCount.Elapsed += (obj, args) =>
			{
				RunOnUiThread(() => { 

					if (_currentLocation != null && update) {
						if(_currentLocation.Latitude <= 53.212933 && _currentLocation.Latitude >= 53.21155 && _currentLocation.Longitude <= 5.800883 && _currentLocation.Longitude >= 5.797874)						{
							presence.Text = String.Format ("Wel");
							presence.SetTextColor(Color.Green);
							imageView.SetImageResource (Resource.Drawable.nhl_light);
							WebClient client = new WebClient();
							Uri uri = new Uri("http://82.73.15.137/?action=location"); //Webserver Raspberry Pi
							NameValueCollection parameters = new NameValueCollection();


							parameters.Add("email", adres);
							parameters.Add("latitude", Convert.ToString(_currentLocation.Latitude));
							parameters.Add("longitude", Convert.ToString(_currentLocation.Longitude));
							parameters.Add("aanwezig", Convert.ToString(true));


							client.UploadValuesAsync(uri, parameters);

						}
						/*Als je niet aanwezig bent, wordt dit in de app aangegeven met een rode tekst. Vervolgens worden er een aantal gegevens
						 * verstuurd naar de webserver.
						 */
						else { 
							presence.Text = String.Format ("Niet");
							presence.SetTextColor(Color.Red);
							imageView.SetImageResource (Resource.Drawable.nhl_dark);
							WebClient client = new WebClient();
							Uri uri = new Uri("http://82.73.15.137/?action=location"); //Webserver Raspberry Pi
							NameValueCollection parameters = new NameValueCollection();


							parameters.Add("email", adres);
							parameters.Add("latitude", Convert.ToString(_currentLocation.Latitude));
							parameters.Add("longitude", Convert.ToString(_currentLocation.Longitude));
							parameters.Add("aanwezig", Convert.ToString(false));	

							client.UploadValuesAsync(uri, parameters);

						
						}
					} else {
						presence.Text = String.Format ("Geen locatie");
						if(update)
						{
							presence.SetTextColor(Color.Red);
						}
						else{
							presence.SetTextColor(Color.DarkGray);
						}
					}
				
				
				}); 
			};
				
			/*Toggle button waarmee je het tracken aan/uit kan zetten. Op basis van of dit aan/uitgeschakeld is veranderen er een paar visuele
			 * elementen in de app.
			*/
			if (toggleUpdate != null) {  // if button exists
				toggleUpdate.Click += (sender, e) => {
					if (toggleUpdate.Checked){
						Toast.MakeText(this, "Teacher Watcher Ingeschakeld", ToastLength.Short).Show ();
						checktest.SetTextColor(Color.WhiteSmoke);
						presence.SetTextColor(Color.Red); 
						update = true;
					}
					else{
						Toast.MakeText(this, "Teacher Watcher Uitgeschakeld", ToastLength.Short).Show ();
						imageView.SetImageResource (Resource.Drawable.nhl_dark);
						checktest.SetTextColor(Color.DarkGray);
						presence.SetTextColor(Color.DarkGray);
						update = false;
					}
				};

			}

			//Login/logout button
			if (login != null) {  // if button exists
				login.Click += (sender, e) => {

					/* Als je nog niet ingelogd bent, en dus op login drukt, wordt er eerst gecontroleerd of het ingevoerde e-mailadres een 
					 * geldig e-mailadres is. Als dit niet het geval is, wordt dit aan de gebruiker gemeld. Als het e-mailadres wel goed is
					 * wordt dit opgeslagen, de timerCount aangezet, de tekst op de knop verandert naar LOGOUT, het e-mailadres invoerveld 
					 * kun je nu niet meer aanpassen, tekstkleuren worden aangepast, en het updaten van de locatie wordt aangezet.
					 */ 
					if(!loggedIn)
					{
						foreach(string x in mailadressen)
						{
							if(editEmail.Text == x){
								adres = editEmail.Text;
								timerCount.Enabled = true;
								login.Text = "logout";
								toggleUpdate.Checked = true;
								editEmail.Enabled = false;
								checktest.SetTextColor(Color.WhiteSmoke);
								presence.SetTextColor(Color.Red); 
								wrongemail = false;
								loggedIn = true;
								update = true;
								toggleUpdate.Enabled = true;
								break;
							}
							else{
								wrongemail = true;
							}
						}
						if(wrongemail){
							Toast.MakeText(this, "Emailadres niet juist!", ToastLength.Short).Show ();
						}
					}
					/* Als je ingelogd bent, en op LOGOUT klikt, wordt het e-mailadres invoerveld geleegd en weer aanpasbaar, de knoptekst 
					 * veranderd naar LOGIN, de kleur van tekstregels aangepast, de afbeelding van het NHL gebouw op donker gezet en de update 
					 * uitgezet.
					 */
					else
					{
						editEmail.Text = "";
						timerCount.Enabled = false;
						login.Text = "login";
						toggleUpdate.Checked = false;
						editEmail.Enabled = true;
						presence.SetTextColor(Color.DarkGray);
						imageView.SetImageResource (Resource.Drawable.nhl_dark);
						checktest.SetTextColor(Color.DarkGray);
						loggedIn = false;
						toggleUpdate.Enabled = false;
						update = false;
					}
				};

			}
				
		}


		//Deze functie initialiseert de LocatieManager
		void InitializeLocationManager()
		{
			_locationManager = (LocationManager) GetSystemService(LocationService);
			Criteria criteriaForLocationService = new Criteria
			{
				Accuracy = Accuracy.Fine
			};
			IList<string> acceptableLocationProviders = _locationManager.GetProviders(criteriaForLocationService, true);

			if (acceptableLocationProviders.Any())
			{
				_locationProvider = acceptableLocationProviders.First();
			}
			else
			{
				_locationProvider = string.Empty;
			}
		}

		//Deze functie maakt het mogelijk om de gegevens op te slaan, zoals e-mailadres, updatestatus en loginstatus.
		protected void saveset(){

			//store
			var prefs = Application.Context.GetSharedPreferences("MyApp", FileCreationMode.Private);
			var prefEditor = prefs.Edit();
			prefEditor.PutString("email", adres);
			prefEditor.PutBoolean ("update", update);
			prefEditor.PutBoolean ("loggedIn", loggedIn);
			prefEditor.Apply ();

		}

		//Deze functie haalt eventueel opgeslagen gegevens op en set de juiste variabelen met de opgeslagen waarden.
		protected void retrieveset()
		{
			//retreive 
			var prefs = Application.Context.GetSharedPreferences("MyApp", FileCreationMode.Private);              
			adres = prefs.GetString("email", null);
			update = prefs.GetBoolean ("update", false);
			loggedIn = prefs.GetBoolean ("loggedIn", false);
			editEmail.Text = adres;
			if (loggedIn) 
			{
				login.Text = "logout";
				editEmail.Enabled = false;
				toggleUpdate.Enabled = true;
				presence.SetTextColor(Color.Red);
				checktest.SetTextColor (Color.WhiteSmoke);
			}
		}

		//Standaardfunctie die is aangepast om de app goed te laten werken als deze naar de achtergrond was verplaatst en weer wordt aangeroepen
        protected override void OnResume()
        {
            base.OnResume();
            _locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
			if (update) {
				toggleUpdate.Checked = true;
				timerCount.Enabled = true;
			}
			if (!update) {
				checktest.SetTextColor (Color.DarkGray);
			}
        }

		//Standaardfunctie die is aangepast om de gegevens op te slaan met saveset(); en de timer uit te zetten.
        protected override void OnPause()
        {
            base.OnPause();
            _locationManager.RemoveUpdates(this);
			saveset ();
			timerCount.Enabled = false;
        }

		//Standaardfunctie die is aangepast om de gegevens op te slaan met saveset()l en de timer uit te zetten.
		protected override void OnStop()
		{
			base.OnStop();
			saveset ();
			timerCount.Enabled = false;
		}

		//Standaardfunctie die is aangepast om de gegevens op te slaan met saveset()l en de timer uit te zetten.
		protected override void OnDestroy()
		{
			base.OnDestroy ();
			saveset ();
			timerCount.Enabled = false;
		}

        
    }
}
