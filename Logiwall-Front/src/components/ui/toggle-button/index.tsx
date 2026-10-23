import { Controller } from 'react-hook-form';
import { ToggleButton, ToggleButtonGroup, Typography } from '@mui/material';
import LgIcon from '../icon';
import { FC } from 'react';

// --------------------------------------------------------------------------------------------

interface ItoggleButton {
  name: string;
  control: any;
  options: { label: string; value: string | number | string[] }[];
}

// --------------------------------------------------------------------------------------------

const LgToggleButton: FC<ItoggleButton> = ({ control, name, options }) => {
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }: any) => (
        <ToggleButtonGroup
          value={field.value}
          exclusive
          onChange={(event, newValue) => {
            field.onChange(newValue);
          }}
          aria-label="text alignment"
          fullWidth
          color="primary"
        >
          {options?.map((option: any) => {
            return (
              <ToggleButton value={option.value} aria-label="right aligned">
                <Typography fontSize={12}>
                  <LgIcon icon="ic:baseline-email" className="inline-block" />
                  {option.label}
                </Typography>
              </ToggleButton>
            );
          })}
        </ToggleButtonGroup>
      )}
    />
  );
};
export default LgToggleButton;
