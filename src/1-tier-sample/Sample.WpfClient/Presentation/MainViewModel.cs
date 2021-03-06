﻿using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Sample.ViewModels;
using Topics.Radical.Windows.Presentation;
using Topics.Radical.Windows.Presentation.ComponentModel;
using Topics.Radical.Windows.Presentation.Services.Validation;
using Radical.CQRS;
using Sample.Domain.People;
using System.Data.Entity;
using Sample.Messages.Commands;
using Jason.Client.ComponentModel;

namespace Sample.WpfClient.Presentation
{
	class MainViewModel : AbstractViewModel, ICanBeValidated, IExpectViewLoadedCallback
	{
		readonly IWorkerServiceClientFactory clientFactory;
		readonly IViewContextFactory<IPeopleViewContext> peopleViewContextFactory;

		public MainViewModel( IWorkerServiceClientFactory clientFactory, IViewContextFactory<IPeopleViewContext> peopleViewContextFactory )
		{
			this.clientFactory = clientFactory;
			this.peopleViewContextFactory = peopleViewContextFactory;
			this.People = new ObservableCollection<PersonView>();
		}

		protected override IValidationService GetValidationService()
		{
			return new DataAnnotationValidationService<MainViewModel>( this );
		}

		[Required( AllowEmptyStrings = false )]
		public String Name
		{
			get { return this.GetPropertyValue( () => this.Name ); }
			set { this.SetInitialPropertyValue( () => this.Name, value ); }
		}

		public ObservableCollection<PersonView> People { get; private set; }

		public async Task CreateNewPerson()
		{
			if( !this.Validate() )
			{
				this.TriggerValidation();
				return;
			}

			using( var client = this.clientFactory.CreateClient() )
			{
				var key = ( Guid )client.Execute( new CreateNewPerson()
				{
					Name = this.Name
				} );

				using( var db = this.peopleViewContextFactory.Create() )
				{
					var result = await db.PeopleView
						.Include( p => p.Addresses )
						.SingleAsync( p => p.Id == key );
					this.People.Insert( 0, result );
				}
			}
		}

		async Task PopulatePeople()
		{
			try
			{
				using( var db = this.peopleViewContextFactory.Create() )
				{
					var all = await db.PeopleView
						.Include( p => p.Addresses )
						.ToListAsync();

					foreach( var item in all )
					{
						this.People.Add( item );
					}
				}
			}
			catch( Exception ex ) 
			{
				throw;
			}
		}

		public void OnViewLoaded()
		{
			this.PopulatePeople();
		}
	}
}