﻿namespace Microsoft.eShopOnContainers.Services.Marketing.API.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.eShopOnContainers.Services.Marketing.API.Infrastructure;
    using System.Threading.Tasks;
    using Microsoft.eShopOnContainers.Services.Marketing.API.Model;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.eShopOnContainers.Services.Marketing.API.Dto;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Authorization;

    [Route("api/v1/[controller]")]
    //[Authorize]
    public class CampaignsController : Controller
    {
        private readonly MarketingContext _context;

        public CampaignsController(MarketingContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCampaigns()
        {
            var campaignList = await _context.Campaigns
                .Include(c => c.Rules)
                .ToListAsync();

            var campaignDtoList = MapCampaignModelListToDtoList(campaignList);

            return Ok(campaignDtoList);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCampaignById(int id)
        {
            var campaign = await _context.Campaigns
                .Include(c => c.Rules)
                .SingleOrDefaultAsync(c => c.Id == id);

            if (campaign is null)
            {
                return NotFound();
            }

            var campaignDto = MapCampaignModelToDto(campaign);

            return Ok(campaignDto);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCampaign([FromBody] CampaignDTO campaignDto)
        {
            if (campaignDto is null)
            {
                return BadRequest();
            }

            var campaingToCreate = MapCampaignDtoToModel(campaignDto);

            await _context.Campaigns.AddAsync(campaingToCreate);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCampaignById), new { id = campaingToCreate.Id }, null);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCampaign(int id, [FromBody]CampaignDTO campaignDto)
        {
            if (id < 1 || campaignDto is null)
            {
                return BadRequest();
            }

            var campaignToUpdate = await _context.Campaigns.FindAsync(id);
            if (campaignToUpdate is null)
            {
                return NotFound();
            }

            campaignToUpdate.Description = campaignDto.Description;
            campaignToUpdate.From = campaignDto.From;
            campaignToUpdate.To = campaignDto.To;
            campaignToUpdate.Url = campaignDto.Url;

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCampaignById), new { id = campaignToUpdate.Id }, null);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id < 1)
            {
                return BadRequest();
            }

            var campaignToDelete = await _context.Campaigns.FindAsync(id);
            if (campaignToDelete is null)
            {
                return NotFound();
            }

            _context.Campaigns.Remove(campaignToDelete);
            await _context.SaveChangesAsync();

            return NoContent();
        }



        private List<CampaignDTO> MapCampaignModelListToDtoList(List<Campaign> campaignList)
        {
            var campaignDtoList = new List<CampaignDTO>();

            campaignList.ForEach(campaign => campaignDtoList
                .Add(MapCampaignModelToDto(campaign)));

            return campaignDtoList;
        }

        private CampaignDTO MapCampaignModelToDto(Campaign campaign)
        {
            var campaignDto = new CampaignDTO
            {
                Id = campaign.Id,
                Description = campaign.Description,
                From = campaign.From,
                To = campaign.To,
                Url = campaign.Url,
            };

            campaign.Rules.ForEach(rule =>
            {
                var ruleDto = new RuleDTO
                {
                    Id = rule.Id,
                    RuleTypeId = rule.RuleTypeId,
                    Description = rule.Description,
                    CampaignId = rule.CampaignId                    
                };

                switch (RuleType.From(rule.RuleTypeId))
                {
                    case RuleTypeEnum.UserLocationRule:
                        var userLocationRule = rule as UserLocationRule;
                        ruleDto.LocationId = userLocationRule.LocationId;
                        break;
                }

                campaignDto.Rules.Add(ruleDto);
            });

            return campaignDto;
        }

        private Campaign MapCampaignDtoToModel(CampaignDTO campaignDto)
        {
            var campaingModel = new Campaign
            {
                Id = campaignDto.Id,
                Description = campaignDto.Description,
                From = campaignDto.From,
                To = campaignDto.To,
                Url = campaignDto.Url
            };

            campaignDto.Rules.ForEach(ruleDto =>
            {
                switch (RuleType.From(ruleDto.RuleTypeId))
                {
                    case RuleTypeEnum.UserLocationRule:
                        campaingModel.Rules.Add(new UserLocationRule
                        {
                            Id = ruleDto.Id,
                            LocationId = ruleDto.LocationId.Value,
                            RuleTypeId = ruleDto.RuleTypeId,
                            Description = ruleDto.Description,
                            Campaign = campaingModel
                        });
                        break;
                }
            });

            return campaingModel;
        }
    }
}