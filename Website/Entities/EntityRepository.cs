﻿using System.Linq;

namespace NuGetGallery
{
    public class EntityRepository<T> : IEntityRepository<T> 
        where T : class, IEntity, new()
    {
        readonly EntitiesContext entities;

        public EntityRepository(EntitiesContext entities)
        {
            this.entities = entities;
        }
        
        public void CommitChanges()
        {
            entities.SaveChanges();
        }

        public void DeleteOnCommit(T entity)
        {
            entities.Set<T>()
                .Remove(entity);
        }

        public T Get(int key)
        {
            return entities.Set<T>()
                .Where(e => e.Key == key)
                .Single();
        }

        public IQueryable<T> GetAll()
        {
            return entities.Set<T>()
                .AsQueryable();
        }

        public int InsertOnCommit(T entity) 
        {
            entities.Set<T>()
                .Add(entity);

            return entity.Key;
        }
    }
}