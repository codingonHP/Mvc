﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class TryUpdateModelIntegrationTest
    {
        private class Address
        {
            public string Street { get; set; }

            public string City { get; set; }
        }

        [Fact]
        public async Task TryUpdateModel_ExistingModel_EmptyPrefix_OverwritesBoundValues()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Address
            {
                Street = "DefaultStreet",
                City = "Toronto",
            };
            var oldModel = model;

            // Act
            var result = await TryUpdateModel(model, string.Empty, operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.Same(oldModel, model);
            Assert.Equal("SomeStreet", model.Street);
            Assert.Equal("Toronto", model.City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        [Fact]
        public async Task TryUpdateModel_ExistingModel_EmptyPrefix_GetsBound()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Address();

            // Act
            var result = await TryUpdateModel(model, string.Empty, operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.Equal("SomeStreet", model.Street);
            Assert.Null(model.City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        private class Person1
        {
            public string Name { get; set; }

            public Address Address { get; set; }
        }

        [Fact]
        public async Task TryUpdateModel_NestedPoco_EmptyPrefix_DoesNotTrounceUnboundValues()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("Address.Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person1
            {
                Name = "Joe",
                Address = new Address
                {
                    Street = "DefaultStreet",
                    City = "Toronto",
                },
            };
            var oldModel = model;

            // Act
            var result = await TryUpdateModel(model, string.Empty, operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.Same(oldModel, model);
            Assert.Equal("Joe", model.Name);
            Assert.Equal("SomeStreet", model.Address.Street);
            Assert.Equal("Toronto", model.Address.City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("Address.Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        private class Person2
        {
            public List<Address> Address { get; set; }
        }

        [Fact]
        public async Task TryUpdateModel_SettableCollectionModel_EmptyPrefix_CreatesCollection()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person2();

            // Act
            var result = await TryUpdateModel(model, string.Empty, operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.NotNull(model.Address);
            Assert.Equal(1, model.Address.Count);
            Assert.Equal("SomeStreet", model.Address[0].Street);
            Assert.Null(model.Address[0].City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("Address[0].Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        [Fact]
        public async Task TryUpdateModel_SettableCollectionModel_EmptyPrefix_MaintainsCollectionIfNonNull()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person2
            {
                Address = new List<Address>(),
            };
            var collection = model.Address;

            // Act
            var result = await TryUpdateModel(model, string.Empty, operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.NotNull(model.Address);
            Assert.Same(collection, model.Address);
            Assert.Equal(1, model.Address.Count);
            Assert.Equal("SomeStreet", model.Address[0].Street);
            Assert.Null(model.Address[0].City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("Address[0].Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        private class Person3
        {
            public Person3()
            {
                Address = new List<Address>();
            }

            public List<Address> Address { get; }
        }

        [Fact]
        public async Task TryUpdateModel_NonSettableCollectionModel_EmptyPrefix_GetsBound()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person3
            {
                Address =
                {
                    new Address
                    {
                        Street = "Old street",
                        City = "Redmond",
                    },
                    new Address
                    {
                        Street = "Older street",
                        City = "Toronto",
                    },
                },
            };

            // Act
            var result = await TryUpdateModel(model, string.Empty, operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model (collection is cleared and new members created from scratch).
            Assert.NotNull(model.Address);
            Assert.Equal(1, model.Address.Count);
            Assert.Equal("SomeStreet", model.Address[0].Street);
            Assert.Null(model.Address[0].City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("Address[0].Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        private class Person6
        {
            public CustomReadOnlyCollection<Address> Address { get; set; }
        }

        [Fact(Skip = "Validation incorrect for collections when using TryUpdateModel, #2941")]
        public async Task TryUpdateModel_ReadOnlyCollectionModel_EmptyPrefix_DoesNotGetBound()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person6();

            // Act
            var result = await TryUpdateModel(model, string.Empty, operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.NotNull(model.Address);

            // Read-only collection should not be updated.
            Assert.Empty(model.Address);

            // ModelState (data is valid but is not copied into Address).
            Assert.True(modelState.IsValid);
            var entry = Assert.Single(modelState);
            Assert.Equal("Address[0].Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
            Assert.Equal("SomeStreet", state.Value.RawValue);
        }

        private class Person4
        {
            public Address[] Address { get; set; }
        }

        [Fact]
        public async Task TryUpdateModel_SettableArrayModel_EmptyPrefix_CreatesArray()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person4();

            // Act
            var result = await TryUpdateModel(model, string.Empty, operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.NotNull(model.Address);
            Assert.Equal(1, model.Address.Length);
            Assert.Equal("SomeStreet", model.Address[0].Street);
            Assert.Null(model.Address[0].City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("Address[0].Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        [Fact]
        public async Task TryUpdateModel_SettableArrayModel_EmptyPrefix_OverwritesArray()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person4
            {
                Address = new Address[]
                {
                    new Address
                    {
                        Street = "Old street",
                        City = "Toronto",
                    },
                },
            };
            var collection = model.Address;

            // Act
            var result = await TryUpdateModel(model, string.Empty, operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.NotNull(model.Address);
            Assert.NotSame(collection, model.Address);
            Assert.Equal(1, model.Address.Length);
            Assert.Equal("SomeStreet", model.Address[0].Street);
            Assert.Null(model.Address[0].City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("Address[0].Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        private class Person5
        {
            public Address[] Address { get; } = new Address[] { };
        }

        [Fact]
        public async Task TryUpdateModel_NonSettableArrayModel_EmptyPrefix_GetsBound()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("Address[0].Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person5();

            // Act
            var result = await TryUpdateModel(model, string.Empty, operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.NotNull(model.Address);

            // Arrays should not be updated.
            Assert.Equal(0, model.Address.Length);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }


        [Fact]
        public async Task TryUpdateModel_ExistingModel_WithPrefix_ValuesGetOverwritten()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("prefix.Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Address
            {
                Street = "DefaultStreet",
                City = "Toronto",
            };
            var oldModel = model;

            // Act
            var result = await TryUpdateModel(model, "prefix", operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.Same(oldModel, model);
            Assert.Equal("SomeStreet", model.Street);
            Assert.Equal("Toronto", model.City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("prefix.Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        [Fact]
        public async Task TryUpdateModel_ExistingModel_WithPrefix_GetsBound()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("prefix.Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Address();

            // Act
            var result = await TryUpdateModel(model, "prefix", operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.Equal("SomeStreet", model.Street);
            Assert.Null(model.City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("prefix.Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        [Fact]
        public async Task TryUpdateModel_NestedPoco_WithPrefix_DoesNotTrounceUnboundValues()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("prefix.Address.Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person1
            {
                Name = "Joe",
                Address = new Address
                {
                    Street = "DefaultStreet",
                    City = "Toronto",
                },
            };
            var oldModel = model;

            // Act
            var result = await TryUpdateModel(model, "prefix", operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.Same(oldModel, model);
            Assert.Equal("Joe", model.Name);
            Assert.Equal("SomeStreet", model.Address.Street);
            Assert.Equal("Toronto", model.Address.City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("prefix.Address.Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        [Fact]
        public async Task TryUpdateModel_SettableCollectionModel_WithPrefix_CreatesCollection()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person2();

            // Act
            var result = await TryUpdateModel(model, "prefix", operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.NotNull(model.Address);
            Assert.Equal(1, model.Address.Count);
            Assert.Equal("SomeStreet", model.Address[0].Street);
            Assert.Null(model.Address[0].City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("prefix.Address[0].Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        [Fact]
        public async Task TryUpdateModel_SettableCollectionModel_WithPrefix_MaintainsCollectionIfNonNull()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person2
            {
                Address = new List<Address>(),
            };
            var collection = model.Address;

            // Act
            var result = await TryUpdateModel(model, "prefix", operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.NotNull(model.Address);
            Assert.Same(collection, model.Address);
            Assert.Equal(1, model.Address.Count);
            Assert.Equal("SomeStreet", model.Address[0].Street);
            Assert.Null(model.Address[0].City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("prefix.Address[0].Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        [Fact]
        public async Task TryUpdateModel_NonSettableCollectionModel_WithPrefix_GetsBound()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person3
            {
                Address =
                {
                    new Address
                    {
                        Street = "Old street",
                        City = "Redmond",
                    },
                    new Address
                    {
                        Street = "Older street",
                        City = "Toronto",
                    },
                },
            };

            // Act
            var result = await TryUpdateModel(model, "prefix", operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model (collection is cleared and new members created from scratch).
            Assert.NotNull(model.Address);
            Assert.Equal(1, model.Address.Count);
            Assert.Equal("SomeStreet", model.Address[0].Street);
            Assert.Null(model.Address[0].City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("prefix.Address[0].Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        [Fact(Skip = "Validation incorrect for collections when using TryUpdateModel, #2941")]
        public async Task TryUpdateModel_ReadOnlyCollectionModel_WithPrefix_DoesNotGetBound()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person6();

            // Act
            var result = await TryUpdateModel(model, "prefix", operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.NotNull(model.Address);

            // Read-only collection should not be updated.
            Assert.Empty(model.Address);

            // ModelState (data is valid but is not copied into Address).
            Assert.True(modelState.IsValid);
            var entry = Assert.Single(modelState);
            Assert.Equal("prefix.Address[0].Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
            Assert.Equal("SomeStreet", state.Value.RawValue);
        }

        [Fact]
        public async Task TryUpdateModel_SettableArrayModel_WithPrefix_CreatesArray()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person4();

            // Act
            var result = await TryUpdateModel(model, "prefix", operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.NotNull(model.Address);
            Assert.Equal(1, model.Address.Length);
            Assert.Equal("SomeStreet", model.Address[0].Street);
            Assert.Null(model.Address[0].City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("prefix.Address[0].Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        [Fact]
        public async Task TryUpdateModel_SettableArrayModel_WithPrefix_OverwritesArray()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person4
            {
                Address = new Address[]
                {
                    new Address
                    {
                        Street = "Old street",
                        City = "Toronto",
                    },
                },
            };
            var collection = model.Address;

            // Act
            var result = await TryUpdateModel(model, "prefix", operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.NotNull(model.Address);
            Assert.NotSame(collection, model.Address);
            Assert.Equal(1, model.Address.Length);
            Assert.Equal("SomeStreet", model.Address[0].Street);
            Assert.Null(model.Address[0].City);

            // ModelState
            Assert.True(modelState.IsValid);

            var entry = Assert.Single(modelState);
            Assert.Equal("prefix.Address[0].Street", entry.Key);
            var state = entry.Value;
            Assert.NotNull(state.Value);
            Assert.Equal("SomeStreet", state.Value.AttemptedValue);
            Assert.Equal("SomeStreet", state.Value.RawValue);
            Assert.Empty(state.Errors);
            Assert.Equal(ModelValidationState.Valid, state.ValidationState);
        }

        [Fact]
        public async Task TryUpdateModel_NonSettableArrayModel_WithPrefix_GetsBound()
        {
            // Arrange
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.QueryString = QueryString.Create("prefix.Address[0].Street", "SomeStreet");
            });

            var modelState = new ModelStateDictionary();
            var model = new Person5();

            // Act
            var result = await TryUpdateModel(model, "prefix", operationContext, modelState);

            // Assert
            Assert.True(result);

            // Model
            Assert.NotNull(model.Address);

            // Arrays should not be updated.
            Assert.Equal(0, model.Address.Length);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        private class CustomReadOnlyCollection<T> : ICollection<T>
        {
            private ICollection<T> _original;

            public CustomReadOnlyCollection()
                : this(new List<T>())
            {
            }

            public CustomReadOnlyCollection(ICollection<T> original)
            {
                _original = original;
            }

            public int Count
            {
                get { return _original.Count; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public void Add(T item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(T item)
            {
                return _original.Contains(item);
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                _original.CopyTo(array, arrayIndex);
            }

            public bool Remove(T item)
            {
                throw new NotSupportedException();
            }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (T t in _original)
                {
                    yield return t;
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private Task<bool> TryUpdateModel(
            object model,
            string prefix,
            OperationBindingContext operationContext,
            ModelStateDictionary modelState)
        {
           return ModelBindingHelper.TryUpdateModelAsync(
               model,
               model.GetType(),
               prefix,
               operationContext.HttpContext,
               modelState,
               operationContext.MetadataProvider,
               operationContext.ModelBinder,
               operationContext.ValueProvider,
               operationContext.InputFormatters,
               ModelBindingTestHelper.GetObjectValidator(),
               operationContext.ValidatorProvider);
        }
    }
}