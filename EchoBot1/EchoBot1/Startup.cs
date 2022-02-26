// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.13.2

using EchoBot1.Bots;
using EchoBot1.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace EchoBot1
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            //Configure services
            services.AddSingleton<BotServices>();
            
            //Configure State
            ConfigureState(services);

            //Instanciamos el Dialogo principal
            ConfigureDialogs(services);

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, DialogBot<MainDialog>>();
        }
        public void ConfigureDialogs(IServiceCollection services)
        {
            services.AddSingleton<MainDialog>();
        }

        public void ConfigureState(IServiceCollection services)
        {
            //Creamos el almacenamiento para el estado del usuario y de la conversacion
            services.AddSingleton<IStorage, MemoryStorage>();

            //Creamos el estado del usuario
            services.AddSingleton<UserState>();

            //Creamos el estado de la conversacion
            services.AddSingleton<ConversationState>();

            //Creamos e instanciamos el estado del servicio
            services.AddSingleton<StateService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseWebSockets()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            // app.UseHttpsRedirection();
        }
    }
}
