﻿using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Hl7.Fhir.Serialization;
using Spark.Engine.Service.FhirServiceExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Spark.Engine.Test
{
    public class PatchServiceTests
    {
        private readonly PatchService _patchService = new PatchService();

        [Fact]
        public void CanReplaceStatusOnMedicationRequest()
        {
            var resource = new MedicationRequest { Id = "test", Status = MedicationRequest.medicationrequestStatus.Active };
            var parameters = new Parameters();
            parameters = parameters.AddReplacePatchParameter("MedicationRequest.status", new Code("completed"));

            resource = (MedicationRequest)_patchService.Apply(resource, parameters);

            Assert.Equal(MedicationRequest.medicationrequestStatus.Completed, resource.Status);
        }

        [Fact]
        public void CanReplacePerformerTypeOnMedicationRequest()
        {
            var resource = new MedicationRequest
            {
                Id = "test",
                PerformerType = new CodeableConcept
                {
                    Coding = new List<Coding>
                    {
                        new Coding
                        {
                            System = "abc",
                             Code = "123",
                        },
                    },
                    Text = "test1",
                }
            };
            var parameters = new Parameters();
            parameters.AddReplacePatchParameter("MedicationRequest.performerType", new CodeableConcept
            {
                Coding = new List<Coding>
                {
                    new Coding
                    {
                        System = "abcd",
                         Code = "1234",
                    },
                },
                Text = "test2",
            });
            parameters.AddReplacePatchParameter("MedicationRequest.id", new Id("test2"));

            resource = (MedicationRequest)_patchService.Apply(resource, parameters);

            Assert.Equal("abcd", resource.PerformerType.Coding[0].System);
            Assert.Equal("1234", resource.PerformerType.Coding[0].Code);
            Assert.Equal("test2", resource.PerformerType.Text);
        }

        [Fact]
        public void CanReplaceSubjectOnMedicationRequest()
        {
            var resource = new MedicationRequest { Id = "test", Subject = new ResourceReference("123") };
            var parameters = new Parameters();
            parameters.AddReplacePatchParameter("MedicationRequest.subject", new ResourceReference("abc"));

            resource = (MedicationRequest)_patchService.Apply(resource, parameters);

            Assert.Equal("abc", resource.Subject.Reference);
        }

        [Fact]
        public void CanAddInstantiatesCanonicalOnMedicationRequest()
        {
            var resource = new MedicationRequest { Id = "test" };
            var parameters = new Parameters();
            parameters.AddAddPatchParameter("MedicationRequest", "instantiatesCanonical", new Canonical("abc"));

            resource = (MedicationRequest)_patchService.Apply(resource, parameters);

            Assert.Equal("abc", resource.InstantiatesCanonical.FirstOrDefault());
        }

        [Fact]
        public void CanAddDosageOnMedicationRequest()
        {
            var resource = new MedicationRequest { Id = "test" };
            var parameters = new Parameters();
            parameters.AddAddPatchParameter("MedicationRequest", "dosageInstruction", new Dosage
            {
                MaxDosePerLifetime = new Quantity
                {
                    Value = 1,
                    Unit = "ml",
                    System = "http://unitsofmeasure.org",
                    Code = "ml",
                }
            });

            resource = (MedicationRequest)_patchService.Apply(resource, parameters);

            Assert.Equal(1m, resource.DosageInstruction[0].MaxDosePerLifetime.Value);
        }

        [Fact]
        public void CanApplyPropertyAssignmentPatch()
        {
            var parameters = new Parameters();
            parameters.AddAddPatchParameter("Patient", "birthDate", new Date("1930-01-01"));

            var resource = new Patient { Id = "test" };
            resource = (Patient)_patchService.Apply(resource, parameters);

            Assert.Equal("1930-01-01", resource.BirthDate);
        }

        [Fact]
        public void WhenApplyingPropertyAssignmentPatchToNonEmptyPropertyThenThrows()
        {
            var parameters = new Parameters();
            parameters.AddAddPatchParameter("Patient", "birthDate", new Date("1930-01-01"));
            var resource = new Patient { Id = "test", BirthDate = "1930-01-01" };

            Assert.Throws<TargetInvocationException>(() => _patchService.Apply(resource, parameters));
        }

        [Fact]
        public void CanApplyCollectionAddPatch()
        {
            var parameters = new Parameters();
            parameters.AddAddPatchParameter("Patient", "name", new HumanName
            {
                Given = new[] { "John" },
                Family = "Doe",
            });

            var resource = new Patient { Id = "test" };
            resource = (Patient)_patchService.Apply(resource, parameters);

            Assert.Equal("John", resource.Name[0].Given.First());
            Assert.Equal("Doe", resource.Name[0].Family);
        }
        
        [Fact]
        public void CanApplyCollectionAddPatchForNonNamedDataTypes()
        {
            var parameters = new Parameters();
            parameters.AddAddPatchParameter("Specimen", "processing", null);
            var valuePart = parameters.Parameter[0].Part[3];
            valuePart.Name = "value";
            valuePart.Part.Add(new Parameters.ParameterComponent()
            {
                Name = "description", Value = new FhirString("testProcessing")
            });
            var dateTime = new FhirDateTime(DateTimeOffset.Now);
            valuePart.Part.Add(new Parameters.ParameterComponent()
            {
                Name = "time", Value = dateTime 
            });

            var resource = new Specimen() { Id = "test" };
            resource = (Specimen)_patchService.Apply(resource, parameters);

            Assert.Equal("testProcessing", resource.Processing[0].Description);
            Assert.Equal(dateTime, resource.Processing[0].Time);
        }

        [Fact]
        public void CanApplyCollectionReplacePatch()
        {
            var resource = new Patient
            {
                Id = "test",
                Name = { new HumanName { Given = new[] { "John" }, Family = "Doe" } }
            };
            var parameters = new Parameters();
            parameters.AddReplacePatchParameter("Patient.name[0]", new HumanName
            {
                Given = new[] { "John" },
                Family = "Johnson",
            });

            resource = (Patient)_patchService.Apply(resource, parameters);

            Assert.Equal("John", resource.Name[0].Given.FirstOrDefault());
            Assert.Equal("Johnson", resource.Name[0].Family);
        }

        [Fact]
        public void CanApplyCollectionInsertPatch()
        {
            var parameters = new Parameters();
            parameters.AddInsertPatchParameter("Patient.name[0]", new HumanName { Given = new[] { "Jane" }, Family = "Doe", }, 0);

            var resource = new Patient
            {
                Id = "test",
                Name = { new HumanName { Given = new[] { "John" }, Family = "Johnson" } }
            };
            resource = (Patient)_patchService.Apply(resource, parameters);

            Assert.Equal("Jane", resource.Name[0].Given.First());
            Assert.Equal("Doe", resource.Name[0].Family);

            Assert.Equal("John", resource.Name[1].Given.First());
            Assert.Equal("Johnson", resource.Name[1].Family);
        }

        [Fact]
        public void CanApplyCollectionMovePatch()
        {
            var parameters = new Parameters();
            parameters.AddMovePatchParameter("Patient.name", 1, 0);

            var resource = new Patient
            {
                Id = "test",
                Name =
                {
                    new HumanName {Given = new[] {"John"}, Family = "Johnson"},
                    new HumanName {Given = new[] {"Jane"}, Family = "Doe"}
                }
            };
            resource = (Patient)_patchService.Apply(resource, parameters);

            Assert.Equal("Jane", resource.Name[0].Given.First());
            Assert.Equal("Doe", resource.Name[0].Family);

            Assert.Equal("John", resource.Name[1].Given.First());
            Assert.Equal("Johnson", resource.Name[1].Family);
        }

        [Fact]
        public void CanApplyPropertyReplacementPatch()
        {
            var parameters = new Parameters();
            parameters.AddReplacePatchParameter("Patient.birthDate", new Date("1930-01-01"));

            var resource = new Patient { Id = "test", BirthDate = "1970-12-24" };
            resource = (Patient)_patchService.Apply(resource, parameters);

            Assert.Equal("1930-01-01", resource.BirthDate);
        }

        [Fact]
        public void CanApplyCollectionDeletePatch()
        {
            var parameters = new Parameters();
            parameters.AddDeletePatchParameter("Patient.name[0]");

            var resource = new Patient { Id = "test", Name = { new HumanName { Text = "John Doe" } } };
            resource = (Patient)_patchService.Apply(resource, parameters);

            Assert.Empty(resource.Name);
        }
    }
}