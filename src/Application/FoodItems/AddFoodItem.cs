using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain;
using Domain.FoodItems;
using FluentResults;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.FoodItems;

public class AddFoodItem
{
    public record Request(FoodItemForm Form, Guid OwnerId) : IRequest<Result<FoodItemDTO>>;

    public class Handler : IRequestHandler<Request, Result<FoodItemDTO>>
    {
        private readonly IApplicationDbContext _db;

        public Handler(IApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Result<FoodItemDTO>> Handle(Request request, CancellationToken cancellationToken)
        {
            var validator = new FoodItemValidator();
            var validationResult = validator.Validate(request.Form);
            if (!validationResult.IsValid)
            {
                return Result.Fail(validationResult.GetErrorList());
            }

            var foodForm = request.Form;
            var nutritionalContent =
                new NutritionalContent(foodForm.Protein, foodForm.Carbohydrate, foodForm.Fat, foodForm.KCal);
            var foodItem = new FoodItem(foodForm.Brand, foodForm.ProductName, nutritionalContent, request.OwnerId);
            _db.FoodItems.Add(foodItem);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return Result.Fail(ex.ToString());
            }
            catch (DbUpdateException ex)
            {
                return Result.Fail(ex.ToString());
            }
            catch (OperationCanceledException)
            {
                return Result.Fail("db operation timed out");
            }

            return Result.Ok(foodItem.ToDTO());
        }
    }
}