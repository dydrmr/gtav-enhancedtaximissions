# GTA V - Enhanced Taxi Missions
A ScriptHookVDotNet mod for Grand Theft Auto V, which aims to improve the taxi driver minigame.



#Features
- Random number of passengers for each trip (between 1 and 3)
- 300 individual pick-up and drop-off locations
- Pick-up locations and destinations change depending on the time of day
- Taxi missions can be played in the following vehicles: Taxi, Stretch, Schafter, Oracle, Washington, Super Drop.
- Player is paid based on distance between pick-up and drop-off locations. Base fare is $6, and every mile driven is $25.
- Customers tip player based on the speed of delivery.

#Planned Features
- More locations
- More tip payment factors based on performance (vehicle damage state, speed of pick-up)
- Celebrity passengers (who tip better)
- Getting hailed by random pedestrians (if driving a taxi)
- Random special missions (e.g. drive Epsilon members from HQ to secret locations / drive Celeb to movie premiere / passenger needs to catch a plane at the airport)

#In-Game
- Press the "L" key to toggle the missions on and off
- If a passenger gets stuck while trying to get into the vehicle, you can use the horn once to get the passenger to retry entering the vehicle. If it still doesn't work, toggle the missions off and on to reset.
- Once you've picked up a passenger, a countdown timer will appear on the UI. As long as it's green, your tip will be 40% of the fare. Once the timer turns yellow, your tip will start to decrease. Once the timer reaches zero, you will not receive a tip. However, you can still complete the mission and receive the base fare.

#Customization
- Edit the EnhancedTaxiMissions.ini file to customize the toggle key and distance units.
- Acceptable values for the toggle key are "A" through "Z", "0" through "1" and "F1" through "F12".
- Acceptable values for unit measurements are "MI" and "KM"
- FAREPERMILE determines how much you get paid per mile driven with a customer.
- AVERAGESPEED determines how fast you need to get to your destination before the countdown timer reaches zero. Larger numbers mean you need to drive faster.
- You can force reload the settings by pressing the "divide" key on your number pad. The script will notify you what settings were found in the ini file.

#Contributions
...are welcome. This is my first gitHub project, still learning the ropes. I'm also a casual programmer, so my code may not be the prettiest to look at!
