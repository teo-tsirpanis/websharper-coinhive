namespace WebSharper.Coinhive

open WebSharper.JavaScript
open WebSharper.InterfaceGenerator

module Definition =
    let Theme =
        Pattern.EnumInlines "Theme" <|
        [
            "Light", "light"
            "Dark", "dark"
        ]
        |> WithComment "The color theme for the opt-in screen - AuthedMine only."

    let OptInStatus =
        Pattern.EnumInlines "OptInStatus" <|
        [
            "Accepted", "accepted"
            "Canceled", "canceled"
        ]
        |> WithComment "Whether the user accepted the opt0in to the mining, or not."

    let Options =
        Pattern.Config "options" {
            Required = []
            Optional =
            [
                "threads", T<byte>
                "throttle", T<float>
                "forceASMJS", T<bool>
                "theme", Theme.Type
                "language", T<string>
            ]
        }

    let StartMode =
        Pattern.EnumInlines "mode" [
            "IfExclusiveTab", "CoinHive.IF_EXCLUSIVE_TAB"
            "ForceExclusiveTab", "CoinHive.FORCE_EXCLUSIVE_TAB"
            "ForceMultiTab", "CoinHive.FORCE_MULTI_TAB"
        ]

    let Coinhive =
        Class "CoinHive"
        |+> Static [
            "Anonymous" => T<string>?siteKey ^-> !?Options ^-> TSelf
            |> WithComment """
Create a new miner that is not attached to a token or user name.
Common use-cases include donations to your website, where users just run the miner without any direct incentives for solved hashes."""

            "User" => T<string>?siteKey ^-> T<string> ^-> !?Options ^-> TSelf
            |> WithComment """Create a new miner and credit all hashes to the specified user name.
You can check a user's balance and withdraw hashes for a user with our [HTTP API](https://coinhive.com/documentation/http-api#user-balance).
Common use-cases include granting in-game currency or other incentives to a user account on your website in turn for running the miner.
Please only use the `CoinHive.User` miner if you later intend to retreive the number of hashes using the HTTP API.
Don't use it to store random session names that you never read back."""

            "Token" => T<string>?siteKey ^-> T<uint32>?targetHashes ^-> !?Options?options ^-> TSelf
            |> WithComment """Create a new miner and stop once the specified number of hashes (`targetHashes`) was found.
Tokens can be verified with our [HTTP API](https://coinhive.com/documentation/http-api#user-balance).
Tokens remain valid for 1 hour after they have reached the target.\nThe random token name is created by our mining pool.
You can read it client side with `getToken()` after the miner successfully authed on the pool.
Common use-cases include one off proof of work verifications to limit actions on your site or grant access to a resource.
For example, this is used by the Coinhive captcha and shortlinks."""
        ]
        |+> Instance [
            "start" => !?StartMode?startMode ^-> T<unit>
            |> WithComment """Connect to the pool and start mining.
The optional mode parameter specifies how the miner should behave if a miner in another tab is already running.
The default is `CoinHive.IF_EXCLUSIVE_TAB`."""
            |> WithSourceName "Start"

            "stop" => T<unit> ^-> T<unit>
            |> WithComment "Stop mining and disconnect from the pool."
            |> WithSourceName "Stop"

            "IsRunning" =? T<bool>
            |> WithGetterInline "$this.isRunning()"
            |> WithComment "Returns whether the miner is currently running: connected to the pool and has working threads."
            |> WithSourceName "IsRunning"

            "IsMobile" =? T<bool>
            |> WithGetterInline "$this.isMobile()"
            |> WithComment "Returns whether the user is using a phone or tablet device. You can use this to only start the miner on laptops and PCs."
            |> WithSourceName "IsMobile"

            "DidOptOut" => !?T<int>?seconds ^-> T<bool>
            |> WithComment """Returns whether the user has clicked the "Cancel" button in the opt-in screen in the last `seconds` seconds.
The seconds parameter is optional and defaults to 14400 (4 hours).
You can use this function to only show the opt-in screen again after a certain time, if the user has canceled the previous opt-in."""
            |> WithSourceName "didOptOut"

            "HasWASMSupport" =? T<bool>
            |> WithGetterInline "$this.hasWASMSupport()"
            |> WithComment """Returns whether the Browser supports WebAssembly.
If WASM is not supported, the miner will automatically use the slower asm.js version.
Consider displaying a warning message to the user to update their browser."""

            "NumThreads" =@ T<int>
            |> WithGetterInline "$this.getNumThreads()"
            |> WithSetterInline "$this.setNumberThreads($0)"
            |> WithComment """Returns the current number of threads. Note that this will report the configured
number of threads, even if the miner is not yet started.

It also sets the desired number of threads. Min: 1. Typically you shouldn't go any
higherthan maybe 8 or 16 threadseven if your users have all new AMD Threadripper CPUs."""

            "Throttle" =@ T<float>
            |> WithGetterInline "$this.getThrottle()"
            |> WithSetterInline "$this.setThrottle($0)"
            |> WithComment """Sets and gets the fraction of time that threads should be idle.
A value of 0 means no throttling (i.e. full speed), a value of 0.5 means that threads will stay idle 50% of the time,
with 0.8 they will stay idle 80% of the time."""

            "Token" =? T<string>
            |> WithGetterInline "$this.getToken()"
            |> WithComment "If the miner was constructed with CoinHive.Token, this returns the token name (string) that was received from the pool.
This token name will be empty until the miner has authed with the pool.
You should listen for the authed event."

            "HashesPerSecond" =? T<int>
            |> WithGetterInline "$this.getHashesPerSecond()"
            |> WithComment "Returns the total number of hashes per second for all threads combined.
Note that each thread typically updates this only once per second."

            "getTotalHashes" => !?T<bool>?interpolate ^-> T<int>
            |> WithSourceName "TotalHashes"
            |> WithComment "Returns the total number of hashes this miner has solved.
Note that this number is typically updated only once per second.
If `interpolate` is true, the miner will estimate the current number of hashes down to the millisecond.
This can be useful if you want to display a fast increasing number to the user, such as in the miner on Coinhive's start page."

            "AcceptedHashes" =? T<int>
            |> WithGetterInline "$this.AcceptedHashes()"
            |> WithComment """Returns the number of hashes that have been accepted by the pool.
For the CoinHive.User miner, this includes all hashes ever accepted for the current user name."""

        ]

    let Assembly =
        Assembly [
            Namespace "WebSharper.Coinhive" [
                Theme
                Options
                StartMode
                Coinhive
            ]
            Namespace "WebSharper.Coinhive.Resources" [
                Resource "Coinhive" "https://coinhive.com/lib/coinhive.min.js"
                |> WithComment """The Coinhive library.
__WARNING__: This library does _not_ inform users that their CPU resources are used to mine Monero,
and it might be blocked by ad-blocking, or worse, antivirus software.
Use `AuthedMine` instead."""

                Resource "AuthedMine" "https://authedmine.com/lib/authedmine.min.js"
                |> WithComment """A version of Coinhive's miner that is not blocked by adblockers but requires an explicit opt-in from the end-user.
[See more here](https://coinhive.com/documentation/authedmine)."""
            ]
        ]

[<Sealed>]
type CoinhiveExtension() =
    interface IExtension with
        member __.Assembly = Definition.Assembly

[<assembly: Extension(typeof<CoinhiveExtension>)>]
do()