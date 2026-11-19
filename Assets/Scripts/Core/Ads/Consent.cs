using System;

namespace ResolverMaster.Core.Ads
{
    /// <summary>
    /// ErNa-013: the privacy consent (Google UMP on the phone). The Settings show "Privacy options" only when
    /// <see cref="PrivacyOptionsRequired"/> is true; there is no consent service in the Editor (button hidden).
    /// </summary>
    public interface IConsentService
    {
        /// <summary>The SDK says the user must be able to change the privacy choices (EEA / UK users).</summary>
        bool PrivacyOptionsRequired { get; }

        /// <summary>Opens the privacy choices form; <paramref name="done"/> runs when it is closed (or could not open).</summary>
        void ShowPrivacyOptions(Action done);

        /// <summary>The consent state changed (form closed, update received): the Settings refresh the button.</summary>
        event Action Changed;
    }
}
