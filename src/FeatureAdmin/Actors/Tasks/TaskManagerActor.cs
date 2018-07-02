﻿using Akka.Actor;
using Akka.Event;
using Caliburn.Micro;
using FeatureAdmin.Core.Models.Tasks;
using System;
using System.Collections.Generic;
using FeatureAdmin.Core.Repository;
using FeatureAdmin.Core.Messages.Request;
using FeatureAdmin.Core.Messages.Completed;
using FeatureAdmin.Messages;
using FeatureAdmin.Core;

namespace FeatureAdmin.Actors.Tasks
{
    public class TaskManagerActor : ReceiveActor
               // ,Caliburn.Micro.IHandle<LoadTask>
               , Caliburn.Micro.IHandle<FeatureToggleRequest>
         , Caliburn.Micro.IHandle<SettingsChanged>
        , Caliburn.Micro.IHandle<Confirmation>
    {
        private readonly ILoggingAdapter _log = Logging.GetLogger(Context);
        private readonly IEventAggregator eventAggregator;
        private readonly IFeatureRepository repository;
        private readonly Dictionary<Guid, IActorRef> taskActors;
        public TaskManagerActor(
            IEventAggregator eventAggregator
            , IFeatureRepository repository
            , bool elevatedPrivileges
            , bool force
            )
        {
            this.eventAggregator = eventAggregator;
            this.eventAggregator.Subscribe(this);
            this.repository = repository;

            this.elevatedPrivileges = elevatedPrivileges;
            this.force = force;

            taskActors = new Dictionary<Guid, IActorRef>();

            Receive<LoadTask>(message => Handle(message));
        }

        private bool elevatedPrivileges { get; set; }
        private bool force { get; set; }
        /// <summary>
        /// send load task to load task actor
        /// </summary>
        /// <param name="message"></param>
        /// <remarks>in the future, this may be enhanced with a start
        /// location, so that it might not only have to be a full farm reload
        /// </remarks>
        public void Handle(LoadTask message)
        {
            IActorRef newTaskActor =
            ActorSystemReference.ActorSystem.ActorOf(LoadTaskActor.Props(eventAggregator, repository,
           message.Id), message.Id.ToString());

            taskActors.Add(message.Id, newTaskActor);

            newTaskActor.Tell(message);
        }

        public void Handle(FeatureToggleRequest message)
        {
            IActorRef newTaskActor =
            ActorSystemReference.ActorSystem.ActorOf(
                FeatureTaskActor.Props(
                    eventAggregator
                    , repository
                    , message.TaskId
                    )
                    );

            taskActors.Add(message.TaskId, newTaskActor);

            var requestWithCorrectSettings = new FeatureToggleRequest(message, force, elevatedPrivileges);

            // trigger feature toggle request
            newTaskActor.Tell(requestWithCorrectSettings);
        }

        public void Handle(SettingsChanged message)
        {
            elevatedPrivileges = message.ElevatedPrivileges;
            force = message.Force;
        }

        /// <summary>
        /// Handles confirmation from a dialog box and forwards to the waiting task
        /// </summary>
        /// <param name="message"></param>
        public void Handle([NotNull] Confirmation message)
        {
            if (taskActors.ContainsKey(message.TaskId))
            {
                taskActors[message.TaskId].Tell(message);
            }
            else
            {
                eventAggregator.PublishOnUIThread(
                    new Messages.LogMessage(Core.Models.Enums.LogLevel.Error,
                    string.Format("Internal error. Confirmed task with task id {0} was not found anymore!",message.TaskId)
                    ));
            }
        }
    }
}