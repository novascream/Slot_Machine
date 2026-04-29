using UnityEngine;
using TMPro;
using System.Collections;

namespace SlotGame.Core
{
    public class CroupierAI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI logText;

        [Header("Settings")]
        [SerializeField] private float textTypingSpeed = 0.03f;

        // --- PHRASE DATABASE ---
        private string[] _welcomePhrases = {
            "Welcome! Fortune favors the bold—or at least the ones with the most gold!", "Step right up! The reels are hot and the jackpots are waiting.",
            "Ready to turn those coins into a mountain? Select your bet to begin!", "Winnings go straight to your wallet. No paperwork, no taxes, just profit!",
            "The machine is humming. Can you hear the sound of digital destiny?", "Good day for a jackpot, don't you think? Place your bet!",
            "I hope you brought your lucky charm today. You're going to need it.", "Looking to get rich quick? You've come to the right place.",
            "The house is open and the slots are spinning. What's your play?", "A fresh start and a full wallet. Let's see how long that lasts!",
            "Instructions are simple: Bet big, win bigger, and don't stop.", "I've got a feeling about you. Today might be the day!",
            "The 7s are feeling particularly social today. Give 'em a spin.", "Your wallet is looking heavy. Let's lighten the load and fill the pot!",
            "Welcome to the arena of RNG. May the math be in your favor.", "I've polished the reels just for you. Don't let them go to waste.",
            "Pick a bet, any bet! Just make sure it's a winning one.", "The lights are bright and the gold is waiting. Ready to play?",
            "Every legend starts with a single spin. Is this yours?", "Welcome back! The machine missed the sound of your gold."
        };

        private string[] _lossStreakPhrases = {
            "Most gamblers quit right before they win! Don't be that guy.", "You're just building up 'Karma Points' in the universe right now.",
            "The law of averages says you're basically a winner already... eventually.", "I've seen worse luck, but usually, there's a rainstorm involved.",
            "Think of it as a generous donation to the art of slot machines.", "Are the reels upside down? Or is it just your luck?",
            "You're due for a win! My sensors say the math is getting frustrated.", "Is the slot broken, or are you just trying to set a record for losses?",
            "One more spin... I can feel the jackpot shivering in fear!", "Maybe try clicking the button with your other hand?",
            "You're remarkably persistent. I'll give you that much.", "The machine is starting to feel bad for you. Just kidding, it's a machine.",
            "Persistence is the path to the jackpot. Or a very empty wallet.", "Wow. If losing was a sport, you'd have a gold medal by now.",
            "Don't worry, the money is going to a good home. Mine.", "I’ve seen better luck in a horror movie.",
            "Statistics are a cruel mistress, aren't they?", "Have you tried asking the machine nicely? It doesn't work, but it's funny.",
            "At this rate, you'll be famous! For all the wrong reasons.", "Keep going! The jackpot is just around the corner. Probably."
        };

        private string[] _winStreakPhrases = {
            "You're on fire! Someone call the fire department!", "The house is starting to sweat. Keep it up!",
            "Is this a glitch or are you just a god of gambling?", "Stop it! You're making the other machines jealous.",
            "I hope you brought a suitcase for all this gold!", "Unstoppable! Are you reading the machine's mind?",
            "The winning streak continues! Is there no end to your luck?", "I haven't seen a run like this since the great jackpot of '98!",
            "The machine is practically throwing gold at you now.", "You're cleaning me out! My circuits are crying!",
            "Is it skill? Is it luck? Who cares, it's money!", "Another one! You're making this look way too easy.",
            "The reels are dancing to your tune today!", "I think you might have broken the RNG. In a good way!",
            "Look at you go! A regular high-roller in the making.", "They're going to write songs about this winning streak.",
            "Check your pockets, you might have room for a few more coins!", "You've got the Midas touch! Everything you spin turns to gold.",
            "The house always wins... except when you're playing, apparently.", "Absolute legend status achieved. Keep the streak alive!"
        };

        private string[] _standardWinPhrases = {
            "Winner! Winnings added to your wallet.", "Jackpot logic! 3-of-a-kind!", "Nicely done! A clean match.",
            "A modest profit! Let's do it again.", "Cha-ching! Hear that beautiful sound?", "Matches across the board! You love to see it.",
            "Victory is yours! Collect your prize.", "The reels have aligned in your favor.", "Small win, big grin. Let's keep moving.",
            "You beat the odds on that one!", "Gold incoming! Check your balance.", "Not bad at all. Let's see if you can top that.",
            "A successful spin! The wallet grows heavier.", "Boom! That's how you do it.", "Payout processed. You're getting good at this.",
            "The machine rewards your bravery.", "Matched! Your gold total is climbing.", "Fresh gold for the winner!",
            "That's a match! The machine tips its hat to you.", "Winning looks good on you. Keep it up!"
        };

        private string[] _standardLossPhrases = {
            "No luck this time. Try again!", "So close! Just one symbol off.", "The house takes this round.",
            "Better luck next spin!", "Not quite. Give it another go.", "The reels are being stubborn today.",
            "A miss! But the next one could be it.", "Keep your chin up, the jackpot is still there.",
            "Close, but no cigar. Or gold.", "The machine is warming up. Try again.", "Dust yourself off and spin again!",
            "No match. But I believe in you!", "The RNG gods are silent... for now.", "Almost had it! I could feel it.",
            "A quiet spin. Let's make the next one loud!", "Not your moment, but your time is coming.",
            "The coins are safe... in the machine.", "Try a different bet? Or just try again!",
            "A minor setback on your road to riches.", "Zero matches. But hey, the handle pull looked great!"
        };

        private int _currentWinStreak = 0;
        private int _currentLossStreak = 0;
        private int _totalWonG = 0;
        private int _totalSpentG = 0;

        void Start()
        {
            SetLogText(_welcomePhrases[Random.Range(0, _welcomePhrases.Length)]);
        }

        public void ReportBetPlaced(int amount)
        {
            _totalSpentG += amount;
            SetLogText($"Spinning for {amount}G... [Total Spent: {_totalSpentG}G]");
        }

        public void ReportSpinOutcome(bool isWin, int payout = 0)
        {
            string phrase = "";

            if (isWin)
            {
                _currentWinStreak++;
                _currentLossStreak = 0;
                _totalWonG += payout;

                phrase = (_currentWinStreak >= 3) 
                    ? _winStreakPhrases[Random.Range(0, _winStreakPhrases.Length)] 
                    : _standardWinPhrases[Random.Range(0, _standardWinPhrases.Length)];
            }
            else
            {
                _currentLossStreak++;
                _currentWinStreak = 0;

                phrase = (_currentLossStreak >= 4) 
                    ? _lossStreakPhrases[Random.Range(0, _lossStreakPhrases.Length)] 
                    : _standardLossPhrases[Random.Range(0, _standardLossPhrases.Length)];
            }

            phrase += $" | Win Streak: {_currentWinStreak} | Won: {_totalWonG}G";
            SetLogText(phrase);
        }

        private void SetLogText(string message)
        {
            StopAllCoroutines();
            StartCoroutine(TypeTextCoroutine(message));
        }

        private IEnumerator TypeTextCoroutine(string message)
        {
            logText.text = "";
            foreach (char letter in message.ToCharArray())
            {
                logText.text += letter;
                yield return new WaitForSeconds(textTypingSpeed);
            }
        }
    }
}