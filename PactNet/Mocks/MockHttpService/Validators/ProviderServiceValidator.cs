﻿using System;
using System.Linq;
using PactNet.Mocks.MockHttpService.Comparers;
using PactNet.Mocks.MockHttpService.Models;
using PactNet.Models;
using PactNet.Reporters;

namespace PactNet.Mocks.MockHttpService.Validators
{
    internal class ProviderServiceValidator : IProviderServiceValidator
    {
        private readonly IProviderServiceResponseComparer _providerServiceResponseComparer;
        private readonly IHttpRequestSender _httpRequestSender;
        private readonly IReporter _reporter;

        internal ProviderServiceValidator(
            IProviderServiceResponseComparer providerServiceResponseComparer,
            IHttpRequestSender httpRequestSender, 
            IReporter reporter)
        {
            _providerServiceResponseComparer = providerServiceResponseComparer;
            _httpRequestSender = httpRequestSender;
            _reporter = reporter;
        }

        public ProviderServiceValidator(
            IHttpRequestSender httpRequestSender, 
            IReporter reporter) : this(
            new ProviderServiceResponseComparer(),
            httpRequestSender,
            reporter)
        {
        }

        public void Validate(ProviderServicePactFile pactFile, ProviderStates providerStates)
        {
            if (pactFile == null)
            {
                throw new ArgumentException("Please supply a non null pactFile");
            }

            if (pactFile.Consumer == null || String.IsNullOrEmpty(pactFile.Consumer.Name))
            {
                throw new ArgumentException("Please supply a non null or empty Consumer name in the pactFile");
            }

            if (pactFile.Provider == null || String.IsNullOrEmpty(pactFile.Provider.Name))
            {
                throw new ArgumentException("Please supply a non null or empty Provider name in the pactFile");
            }

            if (pactFile.Interactions != null && pactFile.Interactions.Any())
            {
                var interationNumber = 1;
                foreach (var interaction in pactFile.Interactions)
                {
                    InvokePactSetUpIfApplicable(providerStates);

                    ProviderState providerStateItem = null;

                    if (interaction.ProviderState != null)
                    {
                        try
                        {
                            providerStateItem = providerStates.Find(interaction.ProviderState);
                        }
                        catch (Exception)
                        {
                            providerStateItem = null;
                        }

                        if (providerStateItem == null)
                        {
                            throw new InvalidOperationException(String.Format("providerState '{0}' was defined by a consumer, however could not be found. Please supply this provider state.", interaction.ProviderState));
                        }
                    }

                    InvokeProviderStateSetUpIfApplicable(providerStateItem);

                    _reporter.ReportInfo(String.Format("{0}) Verifying a Pact between {1} and {2} - {3}.", interationNumber, pactFile.Consumer.Name, pactFile.Provider.Name, interaction.Description));

                    try
                    {
                        ValidateInteraction(interaction);
                    }
                    finally
                    {
                        InvokeProviderStateTearDownIfApplicable(providerStateItem);
                        InvokeTearDownIfApplicable(providerStates);
                    }
                        
                    interationNumber++;
                }

                _reporter.ThrowIfAnyErrors();
            }
        }

        private void ValidateInteraction(ProviderServiceInteraction interaction)
        {
            var expectedResponse = interaction.Response;
            var actualResponse = _httpRequestSender.Send(interaction.Request);

            var responseComparisonResult = _providerServiceResponseComparer.Compare(expectedResponse, actualResponse);
            _reporter.ReportComparisonResult(responseComparisonResult);
        }

        private void InvokePactSetUpIfApplicable(ProviderStates providerStates)
        {
            if (providerStates != null && providerStates.SetUp != null)
            {
                providerStates.SetUp();
            }
        }

        private void InvokeTearDownIfApplicable(ProviderStates providerStates)
        {
            if (providerStates != null && providerStates.TearDown != null)
            {
                providerStates.TearDown();
            }
        }

        private void InvokeProviderStateSetUpIfApplicable(ProviderState providerState)
        {
            if (providerState != null && providerState.SetUp != null)
            {
                providerState.SetUp();
            }
        }

        private void InvokeProviderStateTearDownIfApplicable(ProviderState providerState)
        {
            if (providerState != null && providerState.TearDown != null)
            {
                providerState.TearDown();
            }
        }
    }
}
