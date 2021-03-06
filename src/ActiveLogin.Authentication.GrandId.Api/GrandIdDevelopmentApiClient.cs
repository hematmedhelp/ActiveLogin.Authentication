﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ActiveLogin.Authentication.GrandId.Api.Models;

namespace ActiveLogin.Authentication.GrandId.Api
{
    /// <summary>
    /// Dummy implementation that can be used for development and testing purposes.
    /// </summary>
    public class GrandIdDevelopmentApiClient : IGrandIdApiClient
    {
        private readonly string _givenName;
        private readonly string _surname;
        private string _personalIdentityNumber;
        private TimeSpan _delay = TimeSpan.FromSeconds(2);

        private readonly Dictionary<string, ExtendedFederatedLoginResponse> _federatedLogins = new Dictionary<string, ExtendedFederatedLoginResponse>();
        private readonly Dictionary<string, FederatedDirectLoginResponse> _federatedDirectLogins = new Dictionary<string, FederatedDirectLoginResponse>();

        public GrandIdDevelopmentApiClient() : this("GivenName", "Surname")
        {
        }

        public GrandIdDevelopmentApiClient(string givenName, string surname) : this(givenName, surname, "199908072391")
        {
        }

        public GrandIdDevelopmentApiClient(string givenName, string surname, string personalIdentityNumber)
        {
            _givenName = givenName;
            _surname = surname;
            _personalIdentityNumber = personalIdentityNumber;
        }

        public TimeSpan Delay
        {
            get => _delay;
            set => _delay = value < TimeSpan.Zero ? TimeSpan.Zero : value;
        }

        public async Task<FederatedLoginResponse> FederatedLoginAsync(FederatedLoginRequest request)
        {
            await SimulateResponseDelay().ConfigureAwait(false);

            var sessionId = Guid.NewGuid().ToString();
            var response = new FederatedLoginResponse
            {
                SessionId = sessionId,
                RedirectUrl = $"{request.CallbackUrl}?grandidsession={sessionId}"
            };
            var extendedResponse = new ExtendedFederatedLoginResponse(response, request.PersonalIdentityNumber);
            _federatedLogins.Add(sessionId, extendedResponse);
            return response;
        }

        public async Task<FederatedDirectLoginResponse> FederatedDirectLoginAsync(FederatedDirectLoginRequest request)
        {
            await SimulateResponseDelay().ConfigureAwait(false);

            var sessionId = Guid.NewGuid().ToString();
            var response = new FederatedDirectLoginResponse
            {
                SessionId = sessionId,
                Username = $"{_givenName.ToLower()}.{_surname.ToLower()}@example.org",
                UserAttributes = new FederatedDirectLoginUserAttributes
                {
                    GivenName = _givenName,
                    Surname = _surname,
                    MobilePhone = string.Empty,
                    SameAccountName = $"{_givenName.ToLower()}.{_surname.ToLower()}",
                    Title = "Software Developer"
                }
            };
            _federatedDirectLogins.Add(sessionId, response);
            return response;
        }

        public async Task<SessionStateResponse> GetSessionAsync(SessionStateRequest request)
        {
            await SimulateResponseDelay().ConfigureAwait(false);

            if (!_federatedLogins.ContainsKey(request.SessionId))
            {
                throw new GrandIdApiException(ErrorCode.UNKNOWN, "SessionId not found");
            }

            var auth = _federatedLogins[request.SessionId];
            _federatedLogins.Remove(request.SessionId);

            var personalIdentityNumber = !string.IsNullOrEmpty(auth.PersonalIdentityNumber) ? auth.PersonalIdentityNumber : _personalIdentityNumber;
            var response = new SessionStateResponse
            {
                SessionId = auth.FederatedLoginResponse.SessionId,
                UserAttributes = GetUserAttributes(personalIdentityNumber)
            };

            return response;
        }

        public async Task<LogoutResponse> LogoutAsync(LogoutRequest request)
        {
            await SimulateResponseDelay().ConfigureAwait(false);

            var sessionId = request.SessionId;

            if (_federatedLogins.ContainsKey(sessionId))
            {
                _federatedLogins.Remove(sessionId);
            }

            if (_federatedDirectLogins.ContainsKey(sessionId))
            {
                _federatedDirectLogins.Remove(sessionId);
            }

            return new LogoutResponse()
            {
                SessionDeleted = true
            };
        }

        private SessionUserAttributes GetUserAttributes(string personalIdentityNumber)
        {
            return new SessionUserAttributes
            {
                GivenName = _givenName,
                Surname = _surname,
                Name = $"{_givenName} {_surname}",
                PersonalIdentityNumber = personalIdentityNumber
            };
        }

        private async Task SimulateResponseDelay()
        {
            await Task.Delay(Delay).ConfigureAwait(false);
        }

        void IGrandIdApiClient.SetHsaId(string hsaId)
        {
            _personalIdentityNumber = hsaId;
        }

        private class ExtendedFederatedLoginResponse
        {
            public ExtendedFederatedLoginResponse(FederatedLoginResponse federatedLoginResponse, string personalIdentityNumber)
            {
                FederatedLoginResponse = federatedLoginResponse;
                PersonalIdentityNumber = personalIdentityNumber;
            }

            public FederatedLoginResponse FederatedLoginResponse { get; }
            public string PersonalIdentityNumber { get; }
        }
    }
}