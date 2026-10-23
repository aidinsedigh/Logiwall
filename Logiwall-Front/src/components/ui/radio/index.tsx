import { Controller } from 'react-hook-form';
import {
  FormControl,
  FormControlLabel,
  FormHelperText,
  FormLabel,
  Radio,
  RadioGroup,
  RadioGroupProps,
} from '@mui/material';
import { FC } from 'react';

// ----------------------------------------------------------------------------------
interface IlgRadio extends RadioGroupProps {
  row?: boolean;
  name: string;
  label?: string;
  options?: any;
  spacing?: number;
  helperText?: string;
  disabled?: boolean;
}
// ----------------------------------------------------------------------------------
const LgRadio: FC<IlgRadio> = ({
  row,
  name,
  label,
  options,
  spacing,
  helperText,
  disabled,
  ...other
}) => {
  const labelledby = label ? `${name}-${label}` : '';

  return (
    <Controller
      name={name}
      render={({ field, fieldState: { error } }) => (
        <FormControl>
          {label && (
            <FormLabel component="legend" id={labelledby} sx={{ typography: 'body2' }}>
              {label}
            </FormLabel>
          )}
          <RadioGroup
            {...field}
            className={`${!row && 'space-y-4'}`}
            aria-labelledby={labelledby}
            row={row}
            {...other}
          >
            {options.map((option: { label: string; value: string }) => (
              <FormControlLabel
                key={option.value}
                value={option.value}
                control={<Radio />}
                label={option.label}
                disabled={disabled || false}
                sx={{
                  '&:not(:last-of-type)': {
                    mb: spacing || 0,
                  },
                  ...(row && {
                    mr: 0,
                    '&:not(:last-of-type)': {
                      mr: spacing || 2,
                    },
                  }),
                }}
              />
            ))}
          </RadioGroup>

          {(!!error || helperText) && (
            <FormHelperText error={!!error} sx={{ mx: 0 }}>
              {error ? error?.message : helperText}
            </FormHelperText>
          )}
        </FormControl>
      )}
    />
  );
};
export default LgRadio;
