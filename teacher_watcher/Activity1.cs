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
        static readonly string LogTag = "GetLocation";
        Location _currentLocation;
        LocationManager _locationManager;
		Timer timerCount;
		Button login;

        string _locationProvider;
		EditText editText1;
		ToggleButton toggleUpdate;
		bool update = true;
		string adres = null;
		bool wrongemail;
		bool loggedIn = false;
		List<string> mailadressen = new List<string> {"willem-de-jong@hotmail.com", "mpgalien@gmail.com"};



        public void OnLocationChanged(Location location)
        {
			_currentLocation = location;
			if (timerCount.Enabled == false && adres != null) {
				timerCount.Enabled = true;
			}

        }

        public void OnProviderDisabled(string provider)
        {
        }

        public void OnProviderEnabled(string provider)
        {
        }

        public void OnStatusChanged(string provider, Availability status, Bundle extras)
        {
            Log.Debug(LogTag, "{0}, {1}", provider, status);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Main);

			//TextView _There = FindViewById<TextView>(Resource.Id.checktest);
			ImageView imageView = FindViewById<ImageView> (Resource.Id.demoImageView);
			editText1 = FindViewById<EditText> (Resource.Id.editText1);
			login = FindViewById<Button> (Resource.Id.login);

			toggleUpdate = FindViewById<ToggleButton> (Resource.Id.toggleUpdate);

			InitializeLocationManager();
			toggleUpdate.Checked = true;
			retrieveset ();


			timerCount = new System.Timers.Timer() { Interval = 2000, Enabled = false }; // Interval >= 1000
			timerCount.Elapsed += (obj, args) =>
			{
				RunOnUiThread(() => { 

					TextView _There = FindViewById<TextView>(Resource.Id.checktest1);

					if (_currentLocation != null && update) {
						//double x = 5.54325;
						//double y = 53.23849;
						if(_currentLocation.Latitude <= 53.212933 && _currentLocation.Latitude >= 53.21155 && _currentLocation.Longitude <= 5.800883 && _currentLocation.Longitude >= 5.797874)						{
							_There.Text = String.Format ("Wel");
							_There.SetTextColor(Color.Green);
							imageView.SetImageResource (Resource.Drawable.nhl_light);
							WebClient client = new WebClient();
							Uri uri = new Uri("http://82.73.15.137/?actie=locatie");
							NameValueCollection parameters = new NameValueCollection();



							parameters.Add("email", adres);
							parameters.Add("latitude", Convert.ToString(_currentLocation.Latitude));
							parameters.Add("longitude", Convert.ToString(_currentLocation.Longitude));
							parameters.Add("aanwezig", Convert.ToString(true));


							client.UploadValuesAsync(uri, parameters);

						}
						else { 
							_There.Text = String.Format ("Niet");
							_There.SetTextColor(Color.Red);
							imageView.SetImageResource (Resource.Drawable.nhl_dark);
							WebClient client = new WebClient();
							Uri uri = new Uri("http://82.73.15.137/?actie=locatie");
							NameValueCollection parameters = new NameValueCollection();


							parameters.Add("email", adres);
							parameters.Add("latitude", Convert.ToString(_currentLocation.Latitude));
							parameters.Add("longitude", Convert.ToString(_currentLocation.Longitude));
							parameters.Add("aanwezig", Convert.ToString(false));	

							client.UploadValuesAsync(uri, parameters);

						
						}
					} else {
						_There.Text = String.Format ("Geen locatie");
						_There.SetTextColor(Color.Red);
					}
				
				
				}); 
			};
				

			if (toggleUpdate != null) {  // if button exists
				toggleUpdate.Click += (sender, e) => {
					if (toggleUpdate.Checked){
						Toast.MakeText(this, "Teacher Watcher Ingeschakeld", ToastLength.Short).Show ();
						update = true;
					}
					else{
						Toast.MakeText(this, "Teacher Watcher Uitgeschakeld", ToastLength.Short).Show ();
						update = false;
					}
				};

			}


			if (login != null) {  // if button exists
				login.Click += (sender, e) => {

					if(!loggedIn)
					{
						foreach(string x in mailadressen)
						{
							if(editText1.Text == x){
								adres = editText1.Text;
								timerCount.Enabled = true;
								login.Text = "logout";
								editText1.Enabled = false;
								wrongemail = false;
								loggedIn = true;
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
					else
					{
						editText1.Text = "";
						timerCount.Enabled = false;
						login.Text = "login";
						editText1.Enabled = true;
						loggedIn = false;
					}
				};

			}
				
		}



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
			Log.Debug(LogTag, "Using " + _locationProvider + ".");
		}

		protected void saveset(){

			//store
			var prefs = Application.Context.GetSharedPreferences("MyApp", FileCreationMode.Private);
			var prefEditor = prefs.Edit();
			prefEditor.PutString("email", adres);
			prefEditor.PutBoolean ("update", update);
			prefEditor.PutBoolean ("loggedIn", loggedIn);
			prefEditor.Apply ();

		}

		protected void retrieveset()
		{
			//retreive 
			var prefs = Application.Context.GetSharedPreferences("MyApp", FileCreationMode.Private);              
			adres = prefs.GetString("email", null);
			update = prefs.GetBoolean ("update", false);
			loggedIn = prefs.GetBoolean ("loggedIn", false);
			editText1.Text = adres;
			if (loggedIn) 
			{
				login.Text = "logout";
				editText1.Enabled = false;
			}
		}


        protected override void OnResume()
        {
            base.OnResume();
            _locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this);
            Log.Debug(LogTag, "Listening for location updates using " + _locationProvider + ".");
        }

        protected override void OnPause()
        {
            base.OnPause();
            _locationManager.RemoveUpdates(this);
			saveset ();
            Log.Debug(LogTag, "No longer listening for location updates.");
        }

		protected override void OnStop()
		{
			base.OnStop();
			saveset ();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy ();
			saveset ();
		}

        
    }
}
