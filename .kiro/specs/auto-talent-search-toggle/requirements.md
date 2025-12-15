# Requirements Document

## Introduction

This feature adds an auto talent search toggle in the advisor (军师) menu that allows players to control talent recommendation popup behavior. When enabled, failed talent searches will not show popups and will automatically collect security rewards, while successful talent discoveries will still show popups to alert the player. This feature integrates with the existing talent recommendation system that operates on a yearly frequency.

## Glossary

- **Advisor_System**: The military advisor (军师) interface and functionality system
- **Talent_Recommendation**: The existing yearly talent recruitment recommendation mechanism controlled by LastTalentRecommendYear
- **Faction**: The game entity class that contains AutoTalentSearch and LastTalentRecommendYear properties
- **Auto_Toggle**: The AutoTalentSearch boolean property that controls popup behavior
- **Security_Reward**: The reward automatically granted when talent search fails
- **Popup_Dialog**: The dialog windows that appear to show search results
- **LastTalentRecommendYear**: The property tracking the last year a talent recommendation occurred (controls yearly frequency)

## Requirements

### Requirement 1

**User Story:** As a player, I want to control talent recommendation popup behavior through an advisor menu toggle, so that I can reduce repetitive failure notifications while still being alerted to successful discoveries.

#### Acceptance Criteria

1. WHEN the AutoTalentSearch property is true, THE Advisor_System SHALL suppress failure popups and automatically collect security rewards
2. WHEN the AutoTalentSearch property is true and talent is discovered, THE Advisor_System SHALL still display the success popup
3. WHEN the AutoTalentSearch property is false, THE Advisor_System SHALL maintain original behavior for all search results
4. THE AutoTalentSearch property SHALL persist its state across game sessions through save/load functionality
5. THE Talent_Recommendation SHALL continue to respect the LastTalentRecommendYear frequency control regardless of AutoTalentSearch state

### Requirement 2

**User Story:** As a player, I want to easily access and understand the auto talent search toggle, so that I can make informed decisions about using this feature.

#### Acceptance Criteria

1. WHEN viewing the advisor menu, THE Advisor_System SHALL display the toggle button with clear state indication
2. WHEN hovering over the toggle button, THE Advisor_System SHALL show a tooltip explaining the feature functionality
3. WHEN clicking the toggle button, THE Advisor_System SHALL immediately update the button text and play click sound
4. THE toggle button text SHALL display "自动举荐：[开]" when enabled and "自动举荐：[关]" when disabled

### Requirement 3

**User Story:** As a player, I want the auto talent search feature to integrate seamlessly with existing game systems, so that it doesn't disrupt my normal gameplay experience.

#### Acceptance Criteria

1. WHEN auto talent search is enabled and search fails, THE Talent_Search SHALL automatically process the Security_Reward without user intervention
2. WHEN auto talent search processes rewards automatically, THE Talent_Search SHALL maintain all existing reward calculation logic
3. THE Auto_Toggle SHALL default to disabled state for new games and existing saves without the setting
4. WHEN loading a save file, THE Advisor_System SHALL correctly restore the Auto_Toggle state if present

### Requirement 4

**User Story:** As a developer, I want the auto talent search toggle to be implemented using the existing Faction class structure, so that it integrates seamlessly with the current talent recommendation system.

#### Acceptance Criteria

1. THE Faction class SHALL use the existing AutoTalentSearch boolean property with default value false
2. THE talent recommendation result processing logic SHALL check the AutoTalentSearch state before displaying failure popups
3. THE advisor menu GUI rendering SHALL include the toggle button in the existing menu layout
4. THE implementation SHALL preserve the existing LastTalentRecommendYear frequency control mechanism
5. THE AutoTalentSearch property SHALL not interfere with the yearly recommendation timing controlled by LastTalentRecommendYear