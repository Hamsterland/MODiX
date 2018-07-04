﻿using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Modix.Data.Models;
using Modix.Data.Models.Moderation;

namespace Modix.Data
{
    public class ModixContext : DbContext
    {
        public ModixContext(DbContextOptions<ModixContext> options): base(options) { }
        
        private ModixContext()
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<ModerationAction> ModerationActions { get; set; }

        public DbSet<Infraction> Infractions { get; set; }
        
        public DbSet<DiscordGuild> Guilds { get; set; }
        public DbSet<DiscordMessage> Messages { get; set; }
        public DbSet<DiscordUser> DiscordUsers { get; set; }
        public DbSet<ChannelLimit> ChannelLimits { get; set; }
        public DbSet<PromotionCampaign> PromotionCampaigns { get; set; }
        public DbSet<PromotionComment> PromotionComments { get; set; }

        public bool IsAttached<TEntity>(TEntity entity) where TEntity : class
            => Set<TEntity>().Local.Contains(entity);

        public async Task UpdateEntityPropertiesAsync<TEntity, TProperty>(TEntity entity, params Expression<Func<TEntity, TProperty>>[] propertyExpressions) where TEntity : class
        {
            var isAttached = IsAttached(entity);
            if (!isAttached)
                Attach(entity);

            var entityEntry = Entry(entity);
            foreach(var propertyExpression in propertyExpressions)
                entityEntry.Property(propertyExpression).IsModified = true;
            await SaveChangesAsync();

            if (!isAttached)
                entityEntry.State = EntityState.Detached;
    }
}